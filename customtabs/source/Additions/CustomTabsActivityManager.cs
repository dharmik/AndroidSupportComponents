﻿using System;
using Android.Content;
using Android.OS;
using System.Collections.Generic;
using Android.App;

namespace Android.Support.CustomTabs
{
    public class CustomTabsActivityManager
    {
        static CustomTabsActivityManager instance;
        public static CustomTabsActivityManager From (Activity parentActivity, string servicePackageName = null)
        {
            if (instance == null) {
                instance = new CustomTabsActivityManager (parentActivity);
            }

            return instance;
        }

        public Activity ParentActivity { get; private set; }
        public CustomTabsClient Client { get; private set; }

        CustomTabsSession session = null;
        public CustomTabsSession Session { 
            get {
                if (Client == null) {
                    return null;
                }

                if (session == null) {
                    session = Client.NewSession ((navEvent, extras) => {
                        var evt = NavigationEvent;
                        if (evt != null)
                            evt (navEvent, extras);
                    });
                }

                return session;
            }
        }

        CustomTabsServiceConnectionImpl connection;

        public delegate void NavigationEventDelegate (int navigationEvent, Bundle extras);
        public delegate void CustomTabsServiceConnectedDelegate (ComponentName name, CustomTabsClient client);
        public delegate void CustomTabsServiceDisconnectedDelegate (ComponentName name);

        public event NavigationEventDelegate NavigationEvent;
        public event CustomTabsServiceConnectedDelegate CustomTabsServiceConnected;
        public event CustomTabsServiceDisconnectedDelegate CustomTabsServiceDisconnected;

        public CustomTabsActivityManager (Activity parentActivity)
        {
            ParentActivity = parentActivity;
        }

        public bool BindService (string servicePackageName = null)
        {
            if (string.IsNullOrEmpty (servicePackageName)) {
                servicePackageName = CustomTabsHelper.GetPackageNameToUse (ParentActivity);

                if (servicePackageName == null) 
                    return false;
            }

            connection = new CustomTabsServiceConnectionImpl {
                CustomTabsServiceConnectedHandler = (name, client) => {
                    Client = client;
                    var evt = CustomTabsServiceConnected;
                    if (evt != null)
                        evt (name, client);
                },
                OnServiceDisconnectedHandler = (name) => {
                    var evt = CustomTabsServiceDisconnected;
                    if (evt != null)
                        evt (name);
                }
            };

            return CustomTabsClient.BindCustomTabsService (ParentActivity, servicePackageName, connection);
        }

        public bool Warmup (long flags = 0)
        {
            if (Client == null)
                return false;
            
            return Client.Warmup (flags);
        }

        public bool MayLaunchUrl (string url, Bundle extras, List<string> otherLikelyUrls) 
        {
            if (Session == null)
                return false;
            
            var otherLikelyBundles = new List<Bundle> ();

            if (otherLikelyUrls != null) {
                foreach (var otherUrl in otherLikelyUrls) {
                    var bundle = new Bundle ();
                    bundle.PutString ("url", otherUrl);
                    otherLikelyBundles.Add (bundle);
                }
            }

            return Session.MayLaunchUrl (Android.Net.Uri.Parse (url), extras, otherLikelyBundles);
        }

        public void LaunchUrl (string url, CustomTabsIntent customTabsIntent = null) 
        {            
            if (customTabsIntent == null) {
                customTabsIntent = new CustomTabsIntent.Builder ()
                    .Build ();                    
            }
                
            CustomTabsHelper.AddKeepAliveExtra (ParentActivity, customTabsIntent.Intent);

            customTabsIntent.LaunchUrl (ParentActivity, Android.Net.Uri.Parse (url));
        }

    }
}


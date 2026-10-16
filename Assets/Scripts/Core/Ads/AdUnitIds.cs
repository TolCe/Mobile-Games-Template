using System;
using UnityEngine;

namespace Template.Core.Ads
{
    /// <summary>AdMob ad unit IDs for one platform. Leave a field empty to turn that format off.</summary>
    [Serializable]
    public struct AdUnitIds
    {
        [SerializeField] private string banner;
        [SerializeField] private string interstitial;
        [SerializeField] private string rewarded;

        public AdUnitIds(string banner, string interstitial, string rewarded)
        {
            this.banner = banner;
            this.interstitial = interstitial;
            this.rewarded = rewarded;
        }

        public string Banner => banner;
        public string Interstitial => interstitial;
        public string Rewarded => rewarded;
    }
}

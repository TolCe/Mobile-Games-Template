namespace Template.Core.Ads
{
    public interface IBannerAd
    {
        /// <summary>Creates and loads the banner on the first call, then keeps it visible. Call only after the SDK is initialized.</summary>
        void Show();

        void Hide();

        /// <summary>Destroys the native banner. The next <see cref="Show"/> loads a new one.</summary>
        void Destroy();
    }
}

namespace Objects.Player.SubObjects
{
    /// <summary>
    /// Relays Clicks on SlimeCubes to the Player or other Objects, that have "Ownership" of the Cube rn
    /// </summary>
    public interface IClickRelay
    {
        public void RelayClick(SlimeCube slimeCube);
        void RelayHover(SlimeCube slimeCube);
    }
}
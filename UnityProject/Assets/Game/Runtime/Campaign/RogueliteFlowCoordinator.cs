using System;

namespace OCC.Combat
{
    public sealed class RogueliteFlowCoordinator
    {
        public RogueliteDeveloperRun DeveloperRun { get; private set; }
        public RogueliteMapRun MapRun { get; private set; }
        public bool IsRogueliteMenuOpen { get; private set; }
        public bool IsMapMenuOpen { get; private set; }

        public void OpenRogueliteMenu()
        {
            IsRogueliteMenuOpen = true;
            DeveloperRun = null;
        }

        public void CloseRogueliteMenu() => IsRogueliteMenuOpen = false;

        public void BeginDeveloperRun(RogueliteDeveloperRun run)
        {
            DeveloperRun = run ?? throw new ArgumentNullException(nameof(run));
        }

        public void BeginMapRun(RogueliteMapRun run)
        {
            MapRun = run ?? throw new ArgumentNullException(nameof(run));
            IsMapMenuOpen = true;
            IsRogueliteMenuOpen = false;
            DeveloperRun = null;
        }

        public void SetDeveloperRun(RogueliteDeveloperRun run) => DeveloperRun = run;
        public void SetMapRun(RogueliteMapRun run) => MapRun = run;
        public void SetRogueliteMenuOpen(bool value) => IsRogueliteMenuOpen = value;
        public void SetMapMenuOpen(bool value) => IsMapMenuOpen = value;

        public void ReturnToMap() => IsMapMenuOpen = MapRun != null;

        public void Reset()
        {
            DeveloperRun = null;
            MapRun = null;
            IsRogueliteMenuOpen = false;
            IsMapMenuOpen = false;
        }
    }
}

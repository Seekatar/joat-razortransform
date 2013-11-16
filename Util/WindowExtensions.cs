using System.Windows;

namespace RazorTransform
{
    public static class WindowExtensions
    {
        /// <summary>
        /// try to set the owner if not null, if null sets topmost
        /// </summary>
        /// <param name="me"></param>
        /// <param name="owner"></param>
        /// <param name="center"></param>
        public static void TrySetOwner( this Window me, Window owner, bool center = true )
        {
            if (me != null)
            {
                if (owner != null)
                {
                    me.Owner = owner;
                    if ( center )
                        me.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                }
                else
                {
                    me.Loaded += (sender, args) => { (sender as Window).Topmost = true; };
                    if (center)
                        me.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                }
            }
        }
    }
}

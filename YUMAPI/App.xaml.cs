using System.Windows;
using YUMAPI.Models;

namespace YUMAPI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // On charge les favoris dès le lancement de l'appli
            FavorisManager.Charger();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace YUMAPI.Controllers
{
    // Classe statique = accessible partout sans créer d'objet
    public static class ThemeManager
    {
        // On stocke juste le code couleur en texte
        public static string CouleurHex = "#FF6B35";

        // Propriété qui retourne une nouvelle brosse à chaque fois
        public static SolidColorBrush CouleurAccent
        {
            get
            {
                return new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(CouleurHex)
                );
            }
        }

        public static void ChangerCouleur(string hex)
        {
            CouleurHex = hex;
        }
    }
}

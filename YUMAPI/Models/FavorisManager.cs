using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YUMAPI.Models
{
    public static class FavorisManager
    {
        // Ajoute ou retire une recette des favoris
        public static void BasculerFavori(MealListItem recette)
        {
            User user = UserController.UtilisateurConnecte;
            if (user == null) return;

            if (EstFavori(recette.Id))
                user.Favoris.RemoveAll(r => r.Id == recette.Id);
            else
                user.Favoris.Add(recette);

            UserController.Sauvegarder();
        }

        // Vérifie si une recette est déjà en favori
        public static bool EstFavori(string id)
        {
            User user = UserController.UtilisateurConnecte;
            if (user == null) return false;
            return user.Favoris.Exists(r => r.Id == id);
        }

        // Retourne la liste complète des recettes favorites
        public static List<MealListItem> GetFavoris()
        {
            User user = UserController.UtilisateurConnecte;
            if (user == null) return new List<MealListItem>();
            return user.Favoris;
        }
    }
}
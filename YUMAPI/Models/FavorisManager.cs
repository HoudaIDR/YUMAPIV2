using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using YUMAPI.Models;

namespace YUMAPI.Models
{
    public static class FavorisManager
    {
        // Chemin du fichier de sauvegarde dans le dossier de l'appli
        private static string _fichier = "favoris.json";

        // La liste des favoris en mémoire
        public static List<MealListItem> Favoris = new List<MealListItem>();

        // Charge les favoris depuis le fichier au démarrage
        public static void Charger()
        {
            if (!File.Exists(_fichier)) return;

            string json = File.ReadAllText(_fichier);
            Favoris = JsonSerializer.Deserialize<List<MealListItem>>(json) ?? new List<MealListItem>();
        }

        // Sauvegarde les favoris dans le fichier
        public static void Sauvegarder()
        {
            string json = JsonSerializer.Serialize(Favoris);
            File.WriteAllText(_fichier, json);
        }

        // Ajoute ou retire une recette et sauvegarde
        public static void BasculerFavori(MealListItem recette)
        {
            if (EstFavori(recette.Id))
                Favoris.RemoveAll(r => r.Id == recette.Id);
            else
                Favoris.Add(recette);

            // On sauvegarde à chaque changement
            Sauvegarder();
        }

        // Vérifie si une recette est déjà en favori
        public static bool EstFavori(string id)
        {
            return Favoris.Exists(r => r.Id == id);
        }
    }
}
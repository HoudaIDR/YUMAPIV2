using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using YUMAPI.Models;

namespace YUMAPI.Controllers
{
    /// <summary>
    /// Contrôleur responsable de la communication avec l'API TheMealDB.
    /// Il récupère les recettes et fournit les données aux Views.
    /// </summary>
    public class MealController
    {
        // Client HTTP utilisé pour envoyer les requêtes à l’API
        private HttpClient _client = new HttpClient();

        // URL de base de l’API TheMealDB
        private string _baseUrl = "https://www.themealdb.com/api/json/v1/1/";

        // Liste des recettes (utilisée par la vue ListeView)
        public List<MealListItem> ListeRecettes { get; set; } = new List<MealListItem>();

        // Détail d'une recette (utilisé par la vue DetailView)
        public MealDto DetailRecette { get; set; }

        // ─────────────────────────────────────────────────────────────
        // 🔎 Recherche de recettes par mot-clé
        // ─────────────────────────────────────────────────────────────
        public async Task RechercherAsync(string motCle)
        {
            // Envoi d'une requête GET à l'API (search.php?s=motCle)
            HttpResponseMessage response =
                await _client.GetAsync(_baseUrl + "search.php?s=" + motCle);

            // Si la requête échoue → on arrête
            if (!response.IsSuccessStatusCode)
                return;

            // Lire le contenu JSON retourné par l’API
            string content = await response.Content.ReadAsStringAsync();

            // Désérialiser le JSON en objet C#
            MealSearchResponse data =
                JsonConvert.DeserializeObject<MealSearchResponse>(content);

            // Si aucune donnée trouvée → on arrête
            if (data == null || data.meals == null)
                return;

            // Réinitialiser la liste avant de la remplir
            ListeRecettes = new List<MealListItem>();

            // Convertir chaque MealDto en MealListItem (version simplifiée pour la liste)
            foreach (MealDto dto in data.meals)
            {
                MealListItem item = new MealListItem
                {
                    Id = dto.idMeal,
                    Title = dto.strMeal,
                    Category = dto.strCategory,
                    Thumb = dto.strMealThumb
                };

                ListeRecettes.Add(item);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 📖 Charger le détail complet d'une recette
        // ─────────────────────────────────────────────────────────────
        public async Task ChargerDetailAsync(string id)
        {
            // Requête GET vers lookup.php avec l'ID
            HttpResponseMessage response =
                await _client.GetAsync(_baseUrl + "lookup.php?i=" + id);

            // Si échec → on arrête
            if (!response.IsSuccessStatusCode)
                return;

            // Lire le JSON
            string content = await response.Content.ReadAsStringAsync();

            // Convertir en objet C#
            MealSearchResponse data =
                JsonConvert.DeserializeObject<MealSearchResponse>(content);

            // Vérifier que la recette existe
            if (data != null && data.meals != null && data.meals.Count > 0)
            {
                // On prend le premier résultat (l’ID est unique)
                DetailRecette = data.meals[0];
            }
        }
    }
}
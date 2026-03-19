using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using YUMAPI.Models;

namespace YUMAPI.Controllers
{
    public class MealController
    {
        private HttpClient _client = new HttpClient();
        private string _baseUrl = "https://www.themealdb.com/api/json/v1/1/";

        public List<MealListItem> ListeRecettes { get; set; } = new List<MealListItem>();
        public MealDto DetailRecette { get; set; }

        // Liste des pays reconnus par TheMealDB
        private static readonly HashSet<string> _pays = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "American","British","Canadian","Chinese","Croatian","Dutch","Egyptian",
            "Filipino","French","Greek","Indian","Irish","Italian","Jamaican","Japanese",
            "Kenyan","Malaysian","Mexican","Moroccan","Polish","Portuguese","Russian",
            "Spanish","Thai","Tunisian","Turkish","Ukrainian","Algerian","Lebanese",
            "Vietnamese","Unknown"
        };

        // ─────────────────────────────────────────────────────────────
        // 🔎 Recherche par mot-clé OU par pays (auto-détection)
        // ─────────────────────────────────────────────────────────────
        public async Task RechercherAsync(string motCle)
        {
            // Si le mot-clé est un pays connu → utiliser filter.php?a=
            if (_pays.Contains(motCle))
            {
                await RechercherParPaysAsync(motCle);
                return;
            }

            // Sinon → recherche classique par nom
            HttpResponseMessage response =
                await _client.GetAsync(_baseUrl + "search.php?s=" + motCle);

            if (!response.IsSuccessStatusCode) return;

            string content = await response.Content.ReadAsStringAsync();
            MealSearchResponse data = JsonConvert.DeserializeObject<MealSearchResponse>(content);

            if (data == null || data.meals == null) return;

            ListeRecettes = new List<MealListItem>();
            foreach (MealDto dto in data.meals)
            {
                ListeRecettes.Add(new MealListItem
                {
                    Id = dto.idMeal,
                    Title = dto.strMeal,
                    Category = dto.strCategory,
                    Thumb = dto.strMealThumb
                });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 🌍 Recherche par pays (filter.php?a=)
        // ─────────────────────────────────────────────────────────────
        public async Task RechercherParPaysAsync(string pays)
        {
            HttpResponseMessage response =
                await _client.GetAsync(_baseUrl + "filter.php?a=" + pays);

            if (!response.IsSuccessStatusCode) return;

            string content = await response.Content.ReadAsStringAsync();

            // filter.php retourne un format différent (pas de MealDto complet)
            var data = JsonConvert.DeserializeObject<FilterResponse>(content);

            if (data == null || data.meals == null) return;

            ListeRecettes = new List<MealListItem>();
            foreach (FilterMeal m in data.meals)
            {
                ListeRecettes.Add(new MealListItem
                {
                    Id = m.idMeal,
                    Title = m.strMeal,
                    Thumb = m.strMealThumb
                });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 📖 Charger le détail complet d'une recette
        // ─────────────────────────────────────────────────────────────
        public async Task ChargerDetailAsync(string id)
        {
            HttpResponseMessage response =
                await _client.GetAsync(_baseUrl + "lookup.php?i=" + id);

            if (!response.IsSuccessStatusCode) return;

            string content = await response.Content.ReadAsStringAsync();
            MealSearchResponse data = JsonConvert.DeserializeObject<MealSearchResponse>(content);

            if (data != null && data.meals != null && data.meals.Count > 0)
                DetailRecette = data.meals[0];
        }
    }

    // Classes pour désérialiser filter.php
    public class FilterResponse
    {
        public List<FilterMeal> meals { get; set; }
    }
    public class FilterMeal
    {
        public string strMeal { get; set; }
        public string strMealThumb { get; set; }
        public string idMeal { get; set; }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using YUMAPI.Models;

namespace YUMAPI.Controllers
{
    public class MealController
    {
        private HttpClient _client = new HttpClient();
        private string _baseUrl = "https://www.themealdb.com/api/json/v1/1/";

        // Données que les Views vont lire
        public List<MealListItem> ListeRecettes { get; set; } = new List<MealListItem>();
        public MealDto DetailRecette { get; set; }

        // ── Rechercher par mot-clé ─────────────────────────────────────────
        public async Task RechercherAsync(string motCle)
        {
            HttpResponseMessage response = await _client.GetAsync(_baseUrl + "search.php?s=" + motCle);

            if (!response.IsSuccessStatusCode)
                return;

            string content = await response.Content.ReadAsStringAsync();
            MealSearchResponse data = JsonConvert.DeserializeObject<MealSearchResponse>(content);

            if (data == null || data.meals == null)
                return;

            ListeRecettes = new List<MealListItem>();

            foreach (MealDto dto in data.meals)
            {
                MealListItem item = new MealListItem();
                item.Id = dto.idMeal;
                item.Title = dto.strMeal;
                item.Category = dto.strCategory;
                item.Thumb = dto.strMealThumb;
                ListeRecettes.Add(item);
            }
        }

        // ── Charger le détail d'une recette ───────────────────────────────
        public async Task ChargerDetailAsync(string id)
        {
            HttpResponseMessage response = await _client.GetAsync(_baseUrl + "lookup.php?i=" + id);

            if (!response.IsSuccessStatusCode)
                return;

            string content = await response.Content.ReadAsStringAsync();
            MealSearchResponse data = JsonConvert.DeserializeObject<MealSearchResponse>(content);

            if (data != null && data.meals != null && data.meals.Count > 0)
                DetailRecette = data.meals[0];
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YUMAPI.Models
{
    // Réponse brute de l'API
    public class MealSearchResponse
    {
        public List<MealDto> meals { get; set; }
    }

    // Une recette complète reçue de l'API
    public class MealDto
    {
        public string idMeal { get; set; }
        public string strMeal { get; set; }
        public string strCategory { get; set; }
        public string strArea { get; set; }
        public string strInstructions { get; set; }
        public string strMealThumb { get; set; }

        public string strIngredient1 { get; set; }
        public string strIngredient2 { get; set; }
        public string strIngredient3 { get; set; }
        public string strIngredient4 { get; set; }
        public string strIngredient5 { get; set; }
        public string strIngredient6 { get; set; }
        public string strIngredient7 { get; set; }
        public string strIngredient8 { get; set; }
        public string strIngredient9 { get; set; }
        public string strIngredient10 { get; set; }

        public string strMeasure1 { get; set; }
        public string strMeasure2 { get; set; }
        public string strMeasure3 { get; set; }
        public string strMeasure4 { get; set; }
        public string strMeasure5 { get; set; }
        public string strMeasure6 { get; set; }
        public string strMeasure7 { get; set; }
        public string strMeasure8 { get; set; }
        public string strMeasure9 { get; set; }
        public string strMeasure10 { get; set; }
    }

    // Objet simplifié pour la liste de gauche
    public class MealListItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Thumb { get; set; }
    }
}

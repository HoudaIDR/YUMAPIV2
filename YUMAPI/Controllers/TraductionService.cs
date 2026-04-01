// ============================================================
//  Controllers/TraductionService.cs
//  Traduction + Calories + Ajustement portions via GitHub Models
// ============================================================

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YUMAPI.Models;

namespace YUMAPI.Controllers
{
    public static class TraductionService
    {
        private static string TOKEN => LireToken();
        private const string MODELE = "gpt-4o-mini";
        private const string API_URL = "https://models.inference.ai.azure.com/chat/completions";

        private static string LireToken()
        {
            try
            {
                string chemin = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Tokenignore", "token.txt");
                return File.ReadAllText(chemin).Trim();
            }
            catch { return ""; }
        }

        private static HttpClient _client;

        private static Dictionary<string, MealDto> _cacheTraductions = new Dictionary<string, MealDto>();
        private static Dictionary<string, string> _cacheCalories = new Dictionary<string, string>();
        private static Dictionary<string, string[]> _cachePortions = new Dictionary<string, string[]>();

        public static string LangueActuelle { get; set; } = "en";

        // Vide le cache quand on change de langue
        public static void ViderCache()
        {
            _cacheTraductions.Clear();
            _cacheCalories.Clear();
            _cachePortions.Clear();
        }

        // ── Textes d'interface traduits ───────────────────────────────────
        public static string T(string cle)
        {
            switch (cle)
            {
                case "traduction_en_cours":
                    return LangueActuelle == "fr" ? "Traduction en cours..."
                         : LangueActuelle == "es" ? "Traduciendo..."
                         : "Translating...";
                case "ia_traduit":
                    return LangueActuelle == "fr" ? "L'IA traduit la recette pour vous 🌍"
                         : LangueActuelle == "es" ? "La IA está traduciendo la receta 🌍"
                         : "AI is translating the recipe for you 🌍";
                case "chargement":
                    return LangueActuelle == "fr" ? "Chargement..."
                         : LangueActuelle == "es" ? "Cargando..."
                         : "Loading...";
                case "recuperation":
                    return LangueActuelle == "fr" ? "Récupération de la recette"
                         : LangueActuelle == "es" ? "Obteniendo la receta"
                         : "Fetching recipe";
                case "demarrer_recette":
                    return LangueActuelle == "fr" ? "Démarrer la recette"
                         : LangueActuelle == "es" ? "Iniciar la receta"
                         : "Start recipe";
                case "preparation_ia":
                    return LangueActuelle == "fr" ? "Préparation..."
                         : LangueActuelle == "es" ? "Preparando..."
                         : "Preparing...";
                case "suivant":
                    return LangueActuelle == "fr" ? "Suivant  ▶"
                         : LangueActuelle == "es" ? "Siguiente  ▶"
                         : "Next  ▶";
                case "terminer":
                    return LangueActuelle == "fr" ? "Terminer  ✓"
                         : LangueActuelle == "es" ? "Terminar  ✓"
                         : "Finish  ✓";
                case "quitter":
                    return LangueActuelle == "fr" ? "✕  Quitter"
                         : LangueActuelle == "es" ? "✕  Salir"
                         : "✕  Quit";
                case "precedent":
                    return LangueActuelle == "fr" ? "◀  Précédent"
                         : LangueActuelle == "es" ? "◀  Anterior"
                         : "◀  Previous";
                case "etape":
                    return LangueActuelle == "fr" ? "Étape"
                         : LangueActuelle == "es" ? "Paso"
                         : "Step";
                case "termine":
                    return LangueActuelle == "fr" ? "Terminé !"
                         : LangueActuelle == "es" ? "¡Terminado!"
                         : "Done!";
                case "bonne_degustation":
                    return LangueActuelle == "fr" ? "🎉 Bonne dégustation !\n\nVotre plat est prêt. Profitez bien de votre repas !"
                         : LangueActuelle == "es" ? "🎉 ¡Buen provecho !\n\n¡Tu plato está listo. Disfruta tu comida!"
                         : "🎉 Enjoy your meal!\n\nYour dish is ready. Bon appétit!";
                case "ingredients":
                    return LangueActuelle == "fr" ? "Ingrédients"
                         : LangueActuelle == "es" ? "Ingredientes"
                         : "Ingredients";
                case "preparation":
                    return LangueActuelle == "fr" ? "Préparation"
                         : LangueActuelle == "es" ? "Preparación"
                         : "Preparation";
                default:
                    return cle;
            }
        }

        static TraductionService()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TOKEN);
        }

        // ════════════════════════════════════════════════════════════
        //  TRADUCTION
        // ════════════════════════════════════════════════════════════
        public static async Task<MealDto> TraduireRecette(MealDto recette)
        {
            if (LangueActuelle == "en") return recette;

            string cleCache = $"{recette.idMeal}_{LangueActuelle}";
            if (_cacheTraductions.ContainsKey(cleCache))
                return _cacheTraductions[cleCache];

            string nomLangue = LangueActuelle == "fr" ? "french" : "spanish";

            string prompt =
                $"Translate these recipe fields to {nomLangue}. Reply ONLY with valid JSON, no markdown.\n" +
                $"{{\"titre\":\"{Esc(recette.strMeal)}\"," +
                $"\"instructions\":\"{Esc(recette.strInstructions)}\"," +
                $"\"i1\":\"{Esc(recette.strIngredient1)}\",\"i2\":\"{Esc(recette.strIngredient2)}\"," +
                $"\"i3\":\"{Esc(recette.strIngredient3)}\",\"i4\":\"{Esc(recette.strIngredient4)}\"," +
                $"\"i5\":\"{Esc(recette.strIngredient5)}\",\"i6\":\"{Esc(recette.strIngredient6)}\"," +
                $"\"i7\":\"{Esc(recette.strIngredient7)}\",\"i8\":\"{Esc(recette.strIngredient8)}\"," +
                $"\"i9\":\"{Esc(recette.strIngredient9)}\",\"i10\":\"{Esc(recette.strIngredient10)}\"}}";

            try
            {
                string texteIA = await AppelerIA(
                    "You are a culinary translator. Translate only the values, never the JSON keys. Reply ONLY with valid JSON.",
                    prompt, 1200);

                texteIA = texteIA.Replace("```json", "").Replace("```", "").Trim();
                using JsonDocument trad = JsonDocument.Parse(texteIA);
                JsonElement root = trad.RootElement;

                MealDto traduite = new MealDto
                {
                    idMeal = recette.idMeal,
                    strMeal = Get(root, "titre", recette.strMeal),
                    strCategory = recette.strCategory,
                    strArea = recette.strArea,
                    strInstructions = Get(root, "instructions", recette.strInstructions),
                    strMealThumb = recette.strMealThumb,
                    strIngredient1 = Get(root, "i1", recette.strIngredient1),
                    strIngredient2 = Get(root, "i2", recette.strIngredient2),
                    strIngredient3 = Get(root, "i3", recette.strIngredient3),
                    strIngredient4 = Get(root, "i4", recette.strIngredient4),
                    strIngredient5 = Get(root, "i5", recette.strIngredient5),
                    strIngredient6 = Get(root, "i6", recette.strIngredient6),
                    strIngredient7 = Get(root, "i7", recette.strIngredient7),
                    strIngredient8 = Get(root, "i8", recette.strIngredient8),
                    strIngredient9 = Get(root, "i9", recette.strIngredient9),
                    strIngredient10 = Get(root, "i10", recette.strIngredient10),
                    strMeasure1 = TraduireMesurePublic(recette.strMeasure1),
                    strMeasure2 = TraduireMesurePublic(recette.strMeasure2),
                    strMeasure3 = TraduireMesurePublic(recette.strMeasure3),
                    strMeasure4 = TraduireMesurePublic(recette.strMeasure4),
                    strMeasure5 = TraduireMesurePublic(recette.strMeasure5),
                    strMeasure6 = TraduireMesurePublic(recette.strMeasure6),
                    strMeasure7 = TraduireMesurePublic(recette.strMeasure7),
                    strMeasure8 = TraduireMesurePublic(recette.strMeasure8),
                    strMeasure9 = TraduireMesurePublic(recette.strMeasure9),
                    strMeasure10 = TraduireMesurePublic(recette.strMeasure10),
                };

                _cacheTraductions[cleCache] = traduite;
                return traduite;
            }
            catch { return recette; }
        }

        // ════════════════════════════════════════════════════════════
        //  TRADUCTION DES MESURES
        // ════════════════════════════════════════════════════════════
        public static string TraduireMesurePublic(string mesure)
        {
            if (string.IsNullOrWhiteSpace(mesure)) return mesure;
            if (LangueActuelle == "en") return mesure;

            bool fr = LangueActuelle == "fr";

            string m = mesure.Trim();

            // Remplacements exacts
            var dict = new System.Collections.Generic.Dictionary<string, (string fr, string es)>(StringComparer.OrdinalIgnoreCase)
            {
                { "tablespoon",  ("cuillère à soupe", "cucharada") },
                { "tablespoons", ("cuillères à soupe", "cucharadas") },
                { "tbsp",        ("c. à soupe", "cda.") },
                { "teaspoon",    ("cuillère à café", "cucharadita") },
                { "teaspoons",   ("cuillères à café", "cucharaditas") },
                { "tsp",         ("c. à café", "cdita.") },
                { "cup",         ("tasse", "taza") },
                { "cups",        ("tasses", "tazas") },
                { "pound",       ("livre", "libra") },
                { "pounds",      ("livres", "libras") },
                { "lb",          ("livre", "libra") },
                { "lbs",         ("livres", "libras") },
                { "ounce",       ("once", "onza") },
                { "ounces",      ("onces", "onzas") },
                { "oz",          ("oz", "oz") },
                { "clove",       ("gousse", "diente") },
                { "cloves",      ("gousses", "dientes") },
                { "pinch",       ("pincée", "pizca") },
                { "handful",     ("poignée", "puñado") },
                { "slice",       ("tranche", "rebanada") },
                { "slices",      ("tranches", "rebanadas") },
                { "can",         ("boîte", "lata") },
                { "package",     ("sachet", "paquete") },
                { "bunch",       ("bouquet", "manojo") },
                { "piece",       ("morceau", "pieza") },
                { "pieces",      ("morceaux", "piezas") },
                { "chopped",     ("haché", "picado") },
                { "sliced",      ("tranché", "en rodajas") },
                { "diced",       ("coupé en dés", "en cubos") },
                { "minced",      ("émincé", "picado fino") },
                { "grated",      ("râpé", "rallado") },
                { "finely",      ("finement", "finamente") },
                { "thinly",      ("finement", "finamente") },
                { "large",       ("grand", "grande") },
                { "small",       ("petit", "pequeño") },
                { "medium",      ("moyen", "mediano") },
                { "fresh",       ("frais", "fresco") },
                { "dried",       ("séché", "seco") },
                { "ground",      ("moulu", "molido") },
                { "boneless",    ("désossé", "deshuesado") },
                { "skinless",    ("sans peau", "sin piel") },
            };

            foreach (var kv in dict)
            {
                string mot = kv.Key;
                string trad = fr ? kv.Value.fr : kv.Value.es;
                // Remplacement insensible à la casse avec word boundary
                m = System.Text.RegularExpressions.Regex.Replace(
                    m, @"" + mot + @"", trad,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return m;
        }

        // ════════════════════════════════════════════════════════════
        //  ESTIMATION CALORIES (par personne, base 4 personnes)
        // ════════════════════════════════════════════════════════════
        public static async Task<string> EstimerCalories(MealDto recette)
        {
            if (_cacheCalories.ContainsKey(recette.idMeal))
                return _cacheCalories[recette.idMeal];

            string ingredients = BuildIngredientsList(recette);

            string prompt =
                $"Based on these ingredients for \"{recette.strMeal}\" (4 servings), estimate calories. " +
                $"Reply ONLY with JSON: {{\"par_personne\":\"300 kcal\"}}\n" +
                $"Ingredients: {ingredients}";

            try
            {
                string texteIA = await AppelerIA(
                    "You are a nutritionist. Estimate calories realistically. Reply ONLY with valid JSON.",
                    prompt, 80);

                texteIA = texteIA.Replace("```json", "").Replace("```", "").Trim();
                using JsonDocument doc = JsonDocument.Parse(texteIA);
                string val = doc.RootElement
                    .GetProperty("par_personne").GetString();

                _cacheCalories[recette.idMeal] = val;
                return val;
            }
            catch { return null; }
        }

        // ════════════════════════════════════════════════════════════
        //  AJUSTEMENT PORTIONS
        // ════════════════════════════════════════════════════════════
        public static async Task<string[]> AjusterPortions(MealDto recette, int nbPersonnes)
        {
            // 4 personnes = valeurs originales, pas besoin d'appel IA
            if (nbPersonnes == 4)
            {
                return new string[]
                {
                    recette.strMeasure1,  recette.strMeasure2,
                    recette.strMeasure3,  recette.strMeasure4,
                    recette.strMeasure5,  recette.strMeasure6,
                    recette.strMeasure7,  recette.strMeasure8,
                    recette.strMeasure9,  recette.strMeasure10
                };
            }

            string cle = $"{recette.idMeal}_{nbPersonnes}";
            if (_cachePortions.ContainsKey(cle))
                return _cachePortions[cle];

            string ingredients = BuildIngredientsList(recette);

            string prompt =
                $"Adjust quantities for {nbPersonnes} people (original is for 4). " +
                $"Reply ONLY with a JSON array of exactly 10 strings (adjusted measures), empty string if ingredient is empty.\n" +
                $"Original: {ingredients}\n" +
                $"Example reply: [\"2 tbsp\",\"400g\",\"3\",\"1 tsp\",\"\",\"\",\"\",\"\",\"\",\"\"]";

            try
            {
                string texteIA = await AppelerIA(
                    "You are a chef. Adjust recipe quantities proportionally. Reply ONLY with a JSON array of exactly 10 strings.",
                    prompt, 200);

                texteIA = texteIA.Replace("```json", "").Replace("```", "").Trim();
                using JsonDocument doc = JsonDocument.Parse(texteIA);
                JsonElement arr = doc.RootElement;

                string[] mesures = new string[10];
                for (int i = 0; i < 10 && i < arr.GetArrayLength(); i++)
                    mesures[i] = arr[i].GetString() ?? "";

                _cachePortions[cle] = mesures;
                return mesures;
            }
            catch { return null; }
        }


        // ════════════════════════════════════════════════════════════
        //  PROPOSER UN PLAT ALÉATOIRE (Valeria)
        // ════════════════════════════════════════════════════════════
        public static async Task<string[]> ProposerUnPlat()
        {
            string[] lettres = { "a", "b", "c", "s", "p", "r", "t", "g", "m", "l" };
            try
            {
                Random random = new Random();
                string lettre = lettres[random.Next(lettres.Length)];
                string url = "https://www.themealdb.com/api/json/v1/1/search.php?f=" + lettre;
                HttpResponseMessage rep = await _client.GetAsync(url);
                string repJson = await rep.Content.ReadAsStringAsync();
                using JsonDocument docApi = JsonDocument.Parse(repJson);
                JsonElement meals = docApi.RootElement.GetProperty("meals");
                if (meals.ValueKind == JsonValueKind.Null) return null;
                int nbPlats = meals.GetArrayLength();
                int index = random.Next(nbPlats);
                string vraiNom = meals[index].GetProperty("strMeal").GetString();
                string vraiId = meals[index].GetProperty("idMeal").GetString();
                string[] emojis = { "🍝", "🍜", "🍲", "🥘", "🍛", "🥗", "🍖", "🍗", "🥩", "🫕" };
                string emoji = emojis[random.Next(emojis.Length)];
                string langue = LangueActuelle;
                string phrase = langue == "es" ? "Hoy podríamos cocinar... " + vraiNom + " " + emoji
                              : langue == "fr" ? "Aujourd'hui on pourrait cuisiner... " + vraiNom + " " + emoji
                              : "Today we could cook... " + vraiNom + " " + emoji;
                return new string[] { phrase, vraiId };
            }
            catch { return null; }
        }

        // ════════════════════════════════════════════════════════════
        //  HELPERS PRIVÉS
        // ════════════════════════════════════════════════════════════
        private static async Task<string> AppelerIA(string systemPrompt, string userPrompt, int maxTokens)
        {
            var corps = new
            {
                model = MODELE,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = userPrompt   }
                },
                max_tokens = maxTokens,
                temperature = 0.1
            };

            string json = JsonSerializer.Serialize(corps);
            var contenu = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage rep = await _client.PostAsync(API_URL, contenu);
            string repJson = await rep.Content.ReadAsStringAsync();

            using JsonDocument doc = JsonDocument.Parse(repJson);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }

        // ════════════════════════════════════════════════════════════
        //  DÉCOUPAGE EN ÉTAPES VIA IA
        // ════════════════════════════════════════════════════════════
        public static async Task<List<string>> DecouperEtapesIA(MealDto recette)
        {
            string langue = LangueActuelle == "fr" ? "French" : LangueActuelle == "es" ? "Spanish" : "English";

            string prompt =
                $"Split these cooking instructions into clear numbered steps. " +
                $"Reply in {langue}. " +
                $"Reply ONLY with a JSON array of strings, each string is one step (no step numbers, just the action). " +
                $"Maximum 8 steps. Example: [\"Preheat oven to 200C\",\"Mix flour and eggs\",\"Bake for 30 minutes\"]\n" +
                $"Instructions: {Esc(recette.strInstructions)}";

            try
            {
                string texteIA = await AppelerIA(
                    $"You are a cooking assistant. Split instructions into clear steps in {langue}. Reply ONLY with a JSON array of strings.",
                    prompt, 800);

                texteIA = texteIA.Replace("```json", "").Replace("```", "").Trim();

                using JsonDocument doc = JsonDocument.Parse(texteIA);
                JsonElement arr = doc.RootElement;

                var etapes = new List<string>();
                foreach (JsonElement item in arr.EnumerateArray())
                {
                    string etape = item.GetString();
                    if (!string.IsNullOrWhiteSpace(etape))
                        etapes.Add(etape.Trim());
                }
                return etapes;
            }
            catch { return null; }
        }

        private static string BuildIngredientsList(MealDto r)
        {
            var sb = new StringBuilder();
            void Add(string m, string i) { if (!string.IsNullOrWhiteSpace(i)) sb.Append($"{m} {i}, "); }
            Add(r.strMeasure1, r.strIngredient1); Add(r.strMeasure2, r.strIngredient2);
            Add(r.strMeasure3, r.strIngredient3); Add(r.strMeasure4, r.strIngredient4);
            Add(r.strMeasure5, r.strIngredient5); Add(r.strMeasure6, r.strIngredient6);
            Add(r.strMeasure7, r.strIngredient7); Add(r.strMeasure8, r.strIngredient8);
            Add(r.strMeasure9, r.strIngredient9); Add(r.strMeasure10, r.strIngredient10);
            return sb.ToString().TrimEnd(',', ' ');
        }

        private static string Get(JsonElement root, string cle, string defaut)
        {
            if (root.TryGetProperty(cle, out JsonElement val)) return val.GetString() ?? defaut;
            return defaut ?? "";
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "").Replace("\"", "'").Replace("\n", " ").Replace("\r", "");
        }
    }
}
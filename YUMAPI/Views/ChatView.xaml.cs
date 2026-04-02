// ============================================================
//  Views/ChatView.xaml.cs
//  Chat IA + Reconnaissance vocale (System.Speech)
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Speech.Recognition;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using YUMAPI.Controllers;

namespace YUMAPI.Views
{
    public partial class ChatView : UserControl
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

        private HttpClient _client = new HttpClient();
        private List<object> _historique = new List<object>();
        private SpeechRecognitionEngine _micro;
        private bool _microActif = false;
        private Storyboard _microPulse;
        private static readonly Dictionary<string, string> _traductionsRecettes = new Dictionary<string, string>
        {
            // Cuisines (FR/EN)
            { "algerien", "Algerian" }, { "algérien", "Algerian" }, { "algerian", "Algerian" },
            { "libanais", "Lebanese" }, { "lebanese", "Lebanese" },
            { "marocain", "Moroccan" }, { "moroccan", "Moroccan" },
            { "tunisien", "Tunisian" }, { "tunisian", "Tunisian" },
            { "italien", "Italian" }, { "italian", "Italian" },
            { "japonais", "Japanese" }, { "japanese", "Japanese" },
            { "mexicain", "Mexican" }, { "mexican", "Mexican" },
            { "chinois", "Chinese" }, { "chinese", "Chinese" },
            { "indien", "Indian" }, { "indian", "Indian" },
            { "grec", "Greek" }, { "greek", "Greek" },
            { "francais", "French" }, { "français", "French" }, { "french", "French" },
            { "espagnol", "Spanish" }, { "spanish", "Spanish" },
            { "thai", "Thai" }, { "thaï", "Thai" },
            { "americain", "American" }, { "américain", "American" }, { "american", "American" },
            // Ingrédients / plats
            { "poulet", "chicken" }, { "chicken", "chicken" },
            { "boeuf", "beef" }, { "bœuf", "beef" }, { "beef", "beef" },
            { "poisson", "fish" }, { "fish", "fish" },
            { "agneau", "lamb" }, { "lamb", "lamb" },
            { "pates", "pasta" }, { "pâtes", "pasta" }, { "pasta", "pasta" },
            { "chocolat", "chocolate" }, { "chocolate", "chocolate" }
        };

        public delegate void RechercheRecetteHandler(string motCle);
        public event RechercheRecetteHandler RechercheRecette;

        public delegate void FermerHandler();
        public event FermerHandler Fermer;

        public ChatView()
        {
            InitializeComponent();

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TOKEN);

            _microPulse = (Storyboard)Resources["MicroPulse"];

            // Initialiser le micro
            InitialiserMicro();
        }
            public void Initialiser()
        {
            string l = TraductionService.LangueActuelle;

            TxtStatut.Text = "● " + (l == "es" ? "En línea" : l == "fr" ? "En ligne" : "Online");

            string bienvenue = l == "es"
                ? "¡Hola! 👋 Soy tu asistente culinario Yum!\n\nPuedo ayudarte a:\n• Encontrar recetas con tus ingredientes\n• Sugerir un plato según tus deseos\n\n¡Dime qué quieres comer! 🍽️\nTambién puedes hacer clic en 🎤 para hablar!"
                : l == "fr"
                ? "Bonjour ! 👋 Je suis votre assistant culinaire Yum!\n\nJe peux vous aider à :\n• Trouver une recette avec vos ingrédients\n• Suggérer un plat selon vos envies\n\nDites-moi ce que vous voulez manger ! 🍽️\nVous pouvez aussi cliquer sur 🎤 pour parler !"
                : "Hello! 👋 I'm your Yum! culinary assistant\n\nI can help you:\n• Find recipes with your ingredients\n• Suggest a dish based on your cravings\n\nTell me what you want to eat! 🍽️\nYou can also click 🎤 to speak!";

            AjouterMessageIA(bienvenue);
        }
        

        // ════════════════════════════════════════════════════════════
        //  RECONNAISSANCE VOCALE
        // ════════════════════════════════════════════════════════════
        private void InitialiserMicro()
        {
            try
            {
                _micro = new SpeechRecognitionEngine(
                    new System.Globalization.CultureInfo("fr-FR"));

                // Reconnaissance libre (pas de grammaire fixe)
                _micro.LoadGrammar(new DictationGrammar());

                _micro.SpeechRecognized += OnParoleReconnue;
                _micro.SpeechRecognitionRejected += OnParoleRejetee;
                _micro.RecognizeCompleted += OnReconnaissanceTerminee;

                _micro.SetInputToDefaultAudioDevice();
            }
            catch
            {
                // Micro non disponible → on désactive le bouton
                Dispatcher.InvokeAsync(() =>
                {
                    BoutonMicro.IsEnabled = false;
                    BoutonMicro.Opacity = 0.3;
                    BoutonMicro.ToolTip = "Micro non disponible";
                });
            }
        }

        // ── Clic bouton micro ─────────────────────────────────────────────
        private void BtnMicro_Click(object sender, MouseButtonEventArgs e)
        {
            if (_micro == null) return;

            if (_microActif)
                ArreterMicro();
            else
                DemarrerMicro();
        }

        private void DemarrerMicro()
        {
            try
            {
                _microActif = true;

                // Bouton rouge pulsant
                BoutonMicro.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#CC0000"));
                IconeMicro.Text = "⏹";
                BoutonMicro.ToolTip = "Cliquer pour arrêter";
                _microPulse.Begin();

                string l = TraductionService.LangueActuelle;
                TxtStatut.Text = "● " + (l == "es" ? "Escuchando..." : l == "fr" ? "Écoute en cours..." : "Listening...");
                TxtStatut.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#CC0000"));

                _micro.RecognizeAsync(RecognizeMode.Single);
            }
            catch { ArreterMicro(); }
        }

        private void ArreterMicro()
        {
            try { _micro?.RecognizeAsyncStop(); } catch { }

            _microActif = false;
            _microPulse.Stop();

            Dispatcher.InvokeAsync(() =>
            {
                BoutonMicro.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#222222"));
                IconeMicro.Text = "🎤";
                BoutonMicro.Opacity = 1.0;
                BoutonMicro.ToolTip = "Appuyer pour parler";

                TxtStatut.Text = "● " + (TraductionService.LangueActuelle == "es" ? "En línea"
                                   : TraductionService.LangueActuelle == "fr" ? "En ligne"
                                   : "Online");
                TxtStatut.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#4CAF50"));
            });
        }

        // ── Parole reconnue → met le texte dans la zone de saisie ─────────
        private void OnParoleReconnue(object sender, SpeechRecognizedEventArgs e)
        {
            string texte = e.Result.Text;

            Dispatcher.InvokeAsync(() =>
            {
                InputMessage.Text = texte;
                InputMessage.CaretIndex = texte.Length;
                ArreterMicro();

                // Envoyer automatiquement si confiance > 80%
                if (e.Result.Confidence > 0.80)
                    EnvoyerMessage();
            });
        }

        private void OnParoleRejetee(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                ArreterMicro();
                AjouterMessageIA("Je n'ai pas compris. Pouvez-vous répéter ou écrire votre message ? 🎤");
            });
        }

        private void OnReconnaissanceTerminee(object sender, RecognizeCompletedEventArgs e)
        {
            Dispatcher.InvokeAsync(() => ArreterMicro());
        }

        // ════════════════════════════════════════════════════════════
        //  ENVOI MESSAGE
        // ════════════════════════════════════════════════════════════
        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !e.KeyboardDevice.IsKeyDown(Key.LeftShift))
            {
                e.Handled = true;
                EnvoyerMessage();
            }
        }

        private void BtnEnvoyer_Click(object sender, MouseButtonEventArgs e)
        {
            EnvoyerMessage();
        }

        private async void EnvoyerMessage()
        {
            string texte = InputMessage.Text.Trim();
            if (string.IsNullOrEmpty(texte)) return;

            AjouterMessageUtilisateur(texte);
            InputMessage.Text = "";

            Border indicateur = AjouterIndicateurEcriture();
            string reponse = await AppelerIA(texte);

            PanneauMessages.Children.Remove(indicateur);
            AjouterMessageIA(reponse);

            // Vérifier que la recette existe dans l'API avant de lancer la recherche
            string motCle = ExtraireMotCleRecette(reponse);
            if (!string.IsNullOrEmpty(motCle) && RechercheRecette != null)
            {
                bool existe = await VerifierRecetteExiste(motCle);
                if (existe)
                {
                    RechercheRecette(motCle);
                }
                else
                {
                    // La recette n'existe pas → dire à l'IA de proposer autre chose
                    string msgErreur = TraductionService.LangueActuelle == "es"
                        ? "⚠️ Esa receta no está disponible en nuestra base. ¿Puedes sugerir otra?"
                        : TraductionService.LangueActuelle == "fr"
                            ? "⚠️ Cette recette n'est pas disponible. Peux-tu en suggérer une autre ?"
                            : "⚠️ That recipe isn't available in our database. Can you suggest another one?";
                    AjouterMessageIA(msgErreur);
                }
            }
            // Le chat reste OUVERT
        }

        // ════════════════════════════════════════════════════════════
        //  API IA
        // ════════════════════════════════════════════════════════════
        private async Task<string> AppelerIA(string messageUtilisateur)
        {
            try
            {
                TxtStatut.Text = "● " + (TraductionService.LangueActuelle == "es" ? "Escribiendo..."
                                   : TraductionService.LangueActuelle == "fr" ? "En train d'écrire..."
                                   : "Typing...");
                TxtStatut.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFA500"));

                _historique.Add(new { role = "user", content = messageUtilisateur });

                var corps = new
                {
                    model = MODELE,
                    messages = BuildMessages(),
                    max_tokens = 400,
                    temperature = 0.7
                };

                string json = JsonSerializer.Serialize(corps);
                var contenu = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage rep = await _client.PostAsync(API_URL, contenu);
                string repJson = await rep.Content.ReadAsStringAsync();

                using JsonDocument doc = JsonDocument.Parse(repJson);
                string texteIA = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                _historique.Add(new { role = "assistant", content = texteIA });

                TxtStatut.Text = "● " + (TraductionService.LangueActuelle == "es" ? "En línea"
                                   : TraductionService.LangueActuelle == "fr" ? "En ligne"
                                   : "Online");
                TxtStatut.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#4CAF50"));

                return texteIA;
            }
            catch
            {
                TxtStatut.Text = "● " + (TraductionService.LangueActuelle == "es" ? "En línea"
                                   : TraductionService.LangueActuelle == "fr" ? "En ligne"
                                   : "Online");
                TxtStatut.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#4CAF50"));
                return "Désolé, je n'arrive pas à me connecter. Vérifiez votre connexion.";
            }
        }

        private object[] BuildMessages()
        {
            var messages = new List<object>();

            // Langue dynamique selon le choix de l'utilisateur
            string langue = TraductionService.LangueActuelle == "es" ? "Spanish"
                          : TraductionService.LangueActuelle == "fr" ? "French"
                          : "English";

            // Traductions du message de bienvenue selon la langue
            string cuisinesMsg = langue == "fr"
                ? "marocain→[RECETTE:Moroccan], japonais→[RECETTE:Japanese], italien→[RECETTE:Italian], " +
                  "indien→[RECETTE:Indian], chinois→[RECETTE:Chinese], mexicain→[RECETTE:Mexican], " +
                  "grec→[RECETTE:Greek], américain→[RECETTE:American], thaï→[RECETTE:Thai]"
                : langue == "es"
                ? "marroquí→[RECETTE:Moroccan], japonés→[RECETTE:Japanese], italiano→[RECETTE:Italian], " +
                  "indio→[RECETTE:Indian], chino→[RECETTE:Chinese], mexicano→[RECETTE:Mexican], " +
                  "griego→[RECETTE:Greek], americano→[RECETTE:American], tailandés→[RECETTE:Thai]"
                : "moroccan→[RECETTE:Moroccan], japanese→[RECETTE:Japanese], italian→[RECETTE:Italian], " +
                  "indian→[RECETTE:Indian], chinese→[RECETTE:Chinese], mexican→[RECETTE:Mexican], " +
                  "greek→[RECETTE:Greek], american→[RECETTE:American], thai→[RECETTE:Thai]";

            string ingredientsMsg = langue == "fr"
                ? "poulet→[RECETTE:chicken], boeuf→[RECETTE:beef], pâtes→[RECETTE:pasta], poisson→[RECETTE:fish]"
                : langue == "es"
                ? "pollo→[RECETTE:chicken], ternera→[RECETTE:beef], pasta→[RECETTE:pasta], pescado→[RECETTE:fish]"
                : "chicken→[RECETTE:chicken], beef→[RECETTE:beef], pasta→[RECETTE:pasta], fish→[RECETTE:fish]";

            string nonDispo = langue == "fr"
                ? "Si l'utilisateur demande une cuisine NON DISPONIBLE → dire gentiment que ce n'est pas disponible et proposer une alternative similaire."
                : langue == "es"
                ? "Si el usuario pide una cocina NO DISPONIBLE → decirle amablemente que no está disponible y proponer una alternativa similar."
                : "If the user asks for an UNAVAILABLE cuisine → kindly say it's not available and suggest a similar alternative.";

            string concis = langue == "fr" ? "Réponds en français, de façon chaleureuse. Maximum 3-4 phrases."
                          : langue == "es" ? "Responde en español, de forma amigable. Máximo 3-4 frases."
                          : "Reply in English, in a friendly way. Maximum 3-4 sentences.";

            messages.Add(new
            {
                role = "system",
                content = $"You are a friendly culinary assistant in the Yum! Gourmet Studio app. " +
                          $"CRITICAL: You MUST ALWAYS reply in {langue}. NEVER use another language. " +
                          $"YOUR BEHAVIOR: " +
                          $"- First CONVERSE with the user to understand their tastes and preferences. " +
                          $"- Ask 1-2 questions to understand what they want. " +
                          $"- Only suggest a recipe AFTER understanding their needs. " +
                          $"- IMPORTANT: Only use REAL recipe names or ingredients that exist in TheMealDB database. " +
                          $"- Use SIMPLE English keywords for [RECETTE:...]: 'pasta', 'chicken', 'beef', 'fish', 'cheese', 'chocolate', etc. " +
                          $"- NEVER invent recipe names. Use generic ingredient names as search keywords. " +
                          $"- When confident, say enthusiastically that you found the perfect recipe, then add [RECETTE:keyword]. " +
                          $"- Only include [RECETTE:...] tag when CONFIDENT about what the user wants. " +
                          $"AVAILABLE CUISINES: American, British, Canadian, Chinese, Croatian, Dutch, Egyptian, " +
                          $"Filipino, French, Greek, Indian, Irish, Italian, Jamaican, Japanese, Kenyan, Malaysian, " +
                          $"Mexican, Moroccan, Polish, Portuguese, Russian, Spanish, Thai, Tunisian, Turkish, Ukrainian, Vietnamese. " +
                          $"RULES: " +
                          $"1) Available cuisine → [RECETTE:EnglishName]. Examples: {cuisinesMsg} " +
                          $"2) {nonDispo} " +
                          $"3) Ingredient/dish → translate to English: {ingredientsMsg} " +
                          $"4) Keep responses SHORT (2-3 sentences max). Be warm and enthusiastic. " +
                          $"{concis}"
            });
            messages.AddRange(_historique);
            return messages.ToArray();
        }

        private async System.Threading.Tasks.Task<bool> VerifierRecetteExiste(string motCle)
        {
            try
            {
                var ctrl = new YUMAPI.Controllers.MealController();
                await ctrl.RechercherAsync(motCle);
                return ctrl.ListeRecettes != null && ctrl.ListeRecettes.Count > 0;
            }
            catch { return false; }
        }

        private string ExtraireMotCleRecette(string reponse)
        {
            int debut = reponse.IndexOf("[RECETTE:");
            if (debut == -1) return null;
            int fin = reponse.IndexOf("]", debut);
            if (fin == -1) return null;
            return reponse.Substring(debut + 9, fin - debut - 9).Trim();
        }

        // ════════════════════════════════════════════════════════════
        //  AFFICHAGE MESSAGES
        // ════════════════════════════════════════════════════════════
        private void AjouterMessageUtilisateur(string texte)
        {
            Border bulle = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B35")),
                CornerRadius = new CornerRadius(18, 18, 4, 18),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(60, 0, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Right,
                MaxWidth = 280
            };
            bulle.Child = new TextBlock
            {
                Text = texte,
                Foreground = Brushes.White,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            };
            PanneauMessages.Children.Add(bulle);
            ScrollToBottom();
        }

        private void AjouterMessageIA(string texte)
        {
            string textePropre = Regex.Replace(texte, @"\[RECETTE:[^\]]*\]", "").Trim();

            Grid ligne = new Grid { Margin = new Thickness(0, 0, 60, 12) };
            ligne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            ligne.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border avatar = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B35")),
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 8, 0)
            };
            avatar.Child = new TextBlock
            {
                Text = "🤖",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(avatar, 0);

            Border bulle = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),
                CornerRadius = new CornerRadius(18, 18, 18, 4),
                Padding = new Thickness(14, 10, 14, 10),
                MaxWidth = 280
            };
            bulle.Child = new TextBlock
            {
                Text = textePropre,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEEEEE")),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };
            Grid.SetColumn(bulle, 1);

            ligne.Children.Add(avatar);
            ligne.Children.Add(bulle);
            PanneauMessages.Children.Add(ligne);
            ScrollToBottom();
        }

        private Border AjouterIndicateurEcriture()
        {
            Border bulle = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),
                CornerRadius = new CornerRadius(18, 18, 18, 4),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(40, 0, 60, 12),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            bulle.Child = new TextBlock
            {
                Text = "⏳ " + (TraductionService.LangueActuelle == "es" ? "Escribiendo..." : TraductionService.LangueActuelle == "fr" ? "En train d'écrire..." : "Typing..."),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888")),
                FontSize = 12,
                FontStyle = FontStyles.Italic
            };
            PanneauMessages.Children.Add(bulle);
            ScrollToBottom();
            return bulle;
        }
        public void ReinitialiserEtOuvrir()
        {
            // Vider les messages affichés
            PanneauMessages.Children.Clear();
            // Vider l'historique IA
            _historique.Clear();
            // Afficher le message de bienvenue dans la bonne langue
            Initialiser();
        }
        private void ScrollToBottom()
        {
            Dispatcher.InvokeAsync(() => ScrollMessages.ScrollToEnd(),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void BtnFermer_Click(object sender, MouseButtonEventArgs e)
        {
            ArreterMicro();
            if (Fermer != null) Fermer();
        }
    }
}
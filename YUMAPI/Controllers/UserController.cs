// ============================================================
//  Controllers/UserController.cs
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YUMAPI.Models;

namespace YUMAPI.Models
{
    public static class UserController
    {
        public static User UtilisateurConnecte { get; private set; }
        public static string DerniereErreur { get; private set; }

        private static string _dossier = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "YUMAPI");

        private static string _fichier => Path.Combine(_dossier, "users.json");

        // ── Charger tous les utilisateurs ────────────────────────────────
        private static List<User> ChargerTousLesUtilisateurs()
        {
            if (!Directory.Exists(_dossier))
                Directory.CreateDirectory(_dossier);

            if (!File.Exists(_fichier))
                return new List<User>();

            string json = File.ReadAllText(_fichier);
            return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
        }

        // ── Sauvegarder (met à jour l'utilisateur connecté) ──────────────
        public static void Sauvegarder()
        {
            List<User> utilisateurs = ChargerTousLesUtilisateurs();

            if (UtilisateurConnecte != null)
            {
                int index = utilisateurs.FindIndex(u => u.Username == UtilisateurConnecte.Username);
                if (index >= 0)
                    utilisateurs[index] = UtilisateurConnecte;
            }

            string json = JsonSerializer.Serialize(utilisateurs, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_fichier, json);
        }

        // ── Hasher en MD5 ─────────────────────────────────────────────────
        public static string HasherMotDePasse(string motDePasse)
        {
            byte[] raw = Encoding.UTF8.GetBytes(motDePasse);
            byte[] hash;

            using (MD5 md5 = MD5.Create())
                hash = md5.ComputeHash(raw);

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }

        // ── Créer un compte ───────────────────────────────────────────────
        public static bool Inscrire(string username, string motDePasse)
        {
            List<User> utilisateurs = ChargerTousLesUtilisateurs();

            bool pseudoExiste = utilisateurs.Any(u =>
                u.Username.ToLower() == username.ToLower());

            if (pseudoExiste)
            {
                DerniereErreur = "Ce nom d'utilisateur est déjà pris.";
                return false;
            }

            User nouvelUser = new User();
            nouvelUser.Username = username;
            nouvelUser.Password = HasherMotDePasse(motDePasse);
            nouvelUser.Favoris = new List<MealListItem>();
            nouvelUser.DateCreation = DateTime.Now; // On enregistre la date de création
            nouvelUser.CouleurTheme = "#FF6B35";    // Couleur par défaut : orange

            utilisateurs.Add(nouvelUser);

            string json = JsonSerializer.Serialize(utilisateurs, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            if (!Directory.Exists(_dossier))
                Directory.CreateDirectory(_dossier);

            File.WriteAllText(_fichier, json);

            DerniereErreur = "";
            return true;
        }

        // ── Connexion ─────────────────────────────────────────────────────
        public static bool SeConnecter(string username, string motDePasse)
        {
            List<User> utilisateurs = ChargerTousLesUtilisateurs();

            string hash = HasherMotDePasse(motDePasse);

            User user = utilisateurs.FirstOrDefault(u =>
                u.Username.ToLower() == username.ToLower() &&
                u.Password == hash);

            if (user == null)
            {
                DerniereErreur = "Nom d'utilisateur ou mot de passe incorrect.";
                return false;
            }

            UtilisateurConnecte = user;
            DerniereErreur = "";
            return true;
        }

        // ── Déconnexion ───────────────────────────────────────────────────
        public static void SeDeconnecter()
        {
            UtilisateurConnecte = null;
        }

        // ── Enregistrer la dernière recette consultée ─────────────────────
        public static void EnregistrerDerniereRecette(MealListItem recette)
        {
            if (UtilisateurConnecte == null) return;
            UtilisateurConnecte.DerniereRecette = recette;
            Sauvegarder();
        }
    }
}
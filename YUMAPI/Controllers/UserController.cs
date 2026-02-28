using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography;
using YUMAPI.Models;

namespace YUMAPI.Controllers
{
    public class UserController
    {
        public User UtilisateurConnecte { get; set; }
        public string DerniereErreur { get; set; }

        // ── Hasher le mot de passe en MD5 ─────────────────────────────────
        public string HasherMotDePasse(string motDePasse)
        {
            byte[] raw = Encoding.UTF8.GetBytes(motDePasse);
            byte[] hash;

            using (MD5 md5 = MD5.Create())
            {
                hash = md5.ComputeHash(raw);
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }

            return sb.ToString();
        }

        // ── Créer un compte (username + mot de passe) ──────────────────────
        public bool Inscrire(string username, string motDePasse)
        {
            try
            {
                using (YumapiDbContext context = new YumapiDbContext())
                {
                    // Vérifier si le nom d'utilisateur existe déjà
                    User existant = context.Users
                        .Where(u => u.Username == username)
                        .FirstOrDefault();

                    if (existant != null)
                    {
                        DerniereErreur = "Ce nom d'utilisateur est déjà pris.";
                        return false;
                    }

                    User nouvelUser = new User();
                    nouvelUser.Username = username;
                    nouvelUser.Password = HasherMotDePasse(motDePasse);

                    context.Users.Add(nouvelUser);
                    context.SaveChanges();

                    DerniereErreur = "";
                    return true;
                }
            }
            catch
            {
                DerniereErreur = "Impossible de se connecter à la base de données.\nVérifiez que MySQL est lancé.";
                return false;
            }
        }

        // ── Connexion (username + mot de passe) ────────────────────────────
        public bool SeConnecter(string username, string motDePasse)
        {
            try
            {
                string motDePasseHashe = HasherMotDePasse(motDePasse);

                using (YumapiDbContext context = new YumapiDbContext())
                {
                    User user = context.Users
                        .Where(u => u.Username == username && u.Password == motDePasseHashe)
                        .FirstOrDefault();

                    if (user == null)
                    {
                        DerniereErreur = "Nom d'utilisateur ou mot de passe incorrect.";
                        return false;
                    }

                    UtilisateurConnecte = user;
                    DerniereErreur = "";
                    return true;
                }
            }
            catch
            {
                DerniereErreur = "Impossible de se connecter à la base de données.\nVérifiez que MySQL est lancé.";
                return false;
            }
        }

        // ── Déconnexion ────────────────────────────────────────────────────
        public void SeDeconnecter()
        {
            UtilisateurConnecte = null;
        }
    }
}
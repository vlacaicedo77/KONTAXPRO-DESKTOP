using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace KONTAXPRO.Desktop.Services
{
    public class ThemeService
    {
        private const string LightThemePath =
            "Styles/LightTheme.xaml";

        private const string DarkThemePath =
            "Styles/DarkTheme.xaml";


        /*
         * Carpeta local del usuario de Windows.
         *
         * Ejemplo:
         * C:\Users\Vladimir\AppData\Local\KONTAXPRO
         */
        private static readonly string SettingsFolder =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "KONTAXPRO");


        private static readonly string SettingsFile =
            Path.Combine(
                SettingsFolder,
                "settings.json");


        public bool IsDarkTheme { get; private set; }


        // =========================================================
        // INICIALIZAR TEMA
        // =========================================================

        public void Initialize()
        {
            var preferredTheme =
                LoadThemePreference();

            IsDarkTheme =
                string.Equals(
                    preferredTheme,
                    "Dark",
                    StringComparison.OrdinalIgnoreCase);

            ApplyTheme(
                IsDarkTheme
                    ? DarkThemePath
                    : LightThemePath);
        }


        // =========================================================
        // CAMBIAR TEMA
        // =========================================================

        public void ToggleTheme()
        {
            IsDarkTheme =
                !IsDarkTheme;

            ApplyTheme(
                IsDarkTheme
                    ? DarkThemePath
                    : LightThemePath);

            SaveThemePreference(
                IsDarkTheme
                    ? "Dark"
                    : "Light");
        }


        // =========================================================
        // APLICAR TEMA
        // =========================================================

        private static void ApplyTheme(
            string themePath)
        {
            var dictionaries =
    System.Windows.Application.Current
        .Resources
        .MergedDictionaries;


            var currentTheme =
                dictionaries.FirstOrDefault(
                    dictionary =>
                        dictionary.Source != null &&
                        (
                            dictionary.Source
                                .OriginalString
                                .Contains(
                                    "LightTheme.xaml",
                                    StringComparison.OrdinalIgnoreCase)

                            ||

                            dictionary.Source
                                .OriginalString
                                .Contains(
                                    "DarkTheme.xaml",
                                    StringComparison.OrdinalIgnoreCase)
                        ));


            /*
             * Conservamos la posición en la que estaba
             * cargado el tema.
             */
            var themeIndex =
                currentTheme != null
                    ? dictionaries.IndexOf(currentTheme)
                    : 1;


            if (currentTheme != null)
            {
                dictionaries.Remove(
                    currentTheme);
            }


            /*
             * Evitar índices inválidos en caso de que
             * posteriormente cambie App.xaml.
             */
            themeIndex =
                Math.Clamp(
                    themeIndex,
                    0,
                    dictionaries.Count);


            dictionaries.Insert(
                themeIndex,
                new ResourceDictionary
                {
                    Source =
                        new Uri(
                            themePath,
                            UriKind.Relative)
                });
        }


        // =========================================================
        // LEER PREFERENCIA
        // =========================================================

        private static string LoadThemePreference()
        {
            try
            {
                /*
                 * Primera ejecución.
                 *
                 * Si todavía no existe configuración,
                 * KONTAXPRO inicia en Light.
                 */
                if (!File.Exists(SettingsFile))
                {
                    return "Light";
                }


                var json =
                    File.ReadAllText(
                        SettingsFile);


                var settings =
                    JsonSerializer.Deserialize<AppSettings>(
                        json);


                if (settings == null ||
                    string.IsNullOrWhiteSpace(
                        settings.Theme))
                {
                    return "Light";
                }


                /*
                 * Solo aceptamos los dos valores
                 * que conoce actualmente KONTAXPRO.
                 */
                if (string.Equals(
                    settings.Theme,
                    "Dark",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return "Dark";
                }


                return "Light";
            }
            catch
            {
                /*
                 * Una configuración dañada nunca debe
                 * impedir que KONTAXPRO abra.
                 */
                return "Light";
            }
        }


        // =========================================================
        // GUARDAR PREFERENCIA
        // =========================================================

        private static void SaveThemePreference(
            string theme)
        {
            try
            {
                Directory.CreateDirectory(
                    SettingsFolder);


                var settings =
                    new AppSettings
                    {
                        Theme = theme
                    };


                var json =
                    JsonSerializer.Serialize(
                        settings,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });


                File.WriteAllText(
                    SettingsFile,
                    json);
            }
            catch
            {
                /*
                 * Si Windows no permite escribir la
                 * configuración por cualquier motivo,
                 * el cambio visual se mantiene durante
                 * esta ejecución.
                 *
                 * No tiene sentido cerrar KONTAXPRO
                 * únicamente por no poder guardar
                 * una preferencia visual.
                 */
            }
        }


        // =========================================================
        // CONFIGURACIÓN LOCAL
        // =========================================================

        private sealed class AppSettings
        {
            public string Theme { get; set; } =
                "Light";
        }
    }
}
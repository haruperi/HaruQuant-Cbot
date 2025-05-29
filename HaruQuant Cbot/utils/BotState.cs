using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text.Json;
using cAlgo.API; // For Logger, or a more specific using if Logger is namespaced differently
using System;

namespace cAlgo.Robots.Utils
{
    public class BotState
    {
        public List<string> ActiveTradeLabels { get; set; }
        public double CustomStrategyParameter { get; set; }
        // Add other properties you need to save

        public BotState()
        {
            ActiveTradeLabels = new List<string>();
            // Initialize other properties as needed
        }

        public void Save(Logger logger, string fileName)
        {
            try
            {
                string jsonState = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

                using (IsolatedStorageFile userStore = IsolatedStorageFile.GetUserStoreForAssembly())
                {
                    using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(fileName, FileMode.Create, userStore))
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            writer.Write(jsonState);
                        }
                    }
                }
                logger?.Info("Bot state saved successfully to isolated storage.");
            }
            catch (Exception ex)
            {
                logger?.Error($"Failed to save bot state to isolated storage: {ex.Message}");
            }
        }

        public static BotState Load(Logger logger, string fileName)
        {
            try
            {
                using (IsolatedStorageFile userStore = IsolatedStorageFile.GetUserStoreForAssembly())
                {
                    if (userStore.FileExists(fileName))
                    {
                        using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(fileName, FileMode.Open, userStore))
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                string jsonState = reader.ReadToEnd();
                                var loadedState = JsonSerializer.Deserialize<BotState>(jsonState);
                                if (loadedState != null)
                                {
                                    logger?.Info("Bot state loaded successfully from isolated storage.");
                                    return loadedState;
                                }
                                else
                                {
                                    logger?.Warning("Failed to deserialize bot state from isolated storage. Creating new state.");
                                }
                            }
                        }
                    }
                    else
                    {
                        logger?.Info("No previous bot state file found in isolated storage. Creating new state.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.Error($"Failed to load bot state from isolated storage: {ex.Message}. Creating new state.");
            }
            return new BotState(); // Return a new instance if loading fails or file doesn't exist
        }
    }
} 
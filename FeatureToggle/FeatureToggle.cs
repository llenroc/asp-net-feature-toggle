﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AspNetFeatureToggle.Configuration;

namespace AspNetFeatureToggle
{
    public class FeatureToggle
    {
        private static List<BasicToggleType> FeatureToggles { get; set; }

        private static Random randomGenerator = new Random();

        public static void Initialize()
        {
            Initialize(FeatureToggleSection.Config.FeatureList, new FileUserListReader());
        }

        public static void Initialize(FeatureCollection featureList, IUserListReader userListReader)
        {
            FeatureToggles = new List<BasicToggleType>();
            foreach (FeatureElement feature in featureList)
            {
                var toggleType = CreateToggleType(feature, userListReader);
                FeatureToggles.Add(toggleType);
            }
        }

        public static bool IsEnabled(string featureName)
        {
            if (FeatureToggles == null)
            {
                // Class has not been initialized, run Initialize() and continue.
                Initialize();
            }

            if (string.IsNullOrEmpty(featureName) || !FeatureToggles.Any(f => String.Equals(f.Name, featureName, StringComparison.CurrentCultureIgnoreCase)))
            {
                return false;
            }
            
            foreach (var featureToggle in FeatureToggles)
            {
                if (String.Equals(featureToggle.Name, featureName, StringComparison.CurrentCultureIgnoreCase))
                {
                    var randomToggle = featureToggle as RandomToggleType;
                    if (randomToggle != null)
                    {
                        return featureToggle.Enabled && randomGenerator.NextDouble() <= randomToggle.RandomFactor;
                    }

                    return featureToggle.Enabled;
                }
            }

            return false;
        }

        public static bool IsEnabled(string featureName, string userName)
        {
            return IsEnabled(featureName) && UserIsInFeatureList(featureName, userName);
        }

        private static BasicToggleType CreateToggleType(FeatureElement feature, IUserListReader userListReader)
        {
            BasicToggleType toggleType;

            // The toggle is disabled by default, so if HasValue is false, then toggle is disabled.
            bool enabled = feature.Enabled.HasValue && feature.Enabled.Value;

            if (!string.IsNullOrEmpty(feature.UserListPath))
            {
                var userList = userListReader.GetUserNamesFromList(feature.UserListPath);
                toggleType = new UserListToggleType { Name = feature.Name, Enabled = enabled, UserNamesList = userList };
            }
            else if (!string.IsNullOrEmpty(feature.RandomFactor))
            {
                // Convert string to float
                float factorValue = float.Parse(feature.RandomFactor, CultureInfo.InvariantCulture.NumberFormat);
                toggleType = new RandomToggleType { Name = feature.Name, Enabled = enabled, RandomFactor = factorValue };
            }
            else
            {
                toggleType = new BasicToggleType { Name = feature.Name, Enabled = enabled };
            }

            return toggleType;
        }

        private static bool UserIsInFeatureList(string featureName, string userName)
        {
            return FeatureToggles.Where(featureToggle => String.Equals(featureToggle.Name, featureName, StringComparison.CurrentCultureIgnoreCase))
                                 .OfType<UserListToggleType>()
                                 .Select(userListFeature => userListFeature.UserNamesList.Any(u => String.Equals(u, userName, StringComparison.CurrentCultureIgnoreCase)))
                                 .FirstOrDefault();
        }
    }
}

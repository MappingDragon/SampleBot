using System.Threading.Tasks;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DnDClassCreationBot
{
    public class DnDBot : IBot
    {
        private readonly DialogSet _dialogs;

        static string lotrChar;
        static string rolePreference;
        static string classPreference;
        static string racePreference;
        static string finalResponse;
        bool result = false;

        CharacterBuilder DnDBuilder = new CharacterBuilder();
        //CharacterSuggestion SuggestedChar = new CharacterSuggestion();

        public DnDBot()
        {
            _dialogs = new DialogSet();

            // Define our dialog
            _dialogs.Add("characterBuilder", new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    await dc.Prompt("charPrompt", "What character would you like to play from Lord of the Rings? If you don't know, say so!");
                },
                async(dc, args, next) =>
                {
                    lotrChar = args["Text"].ToString().ToLowerInvariant();
                    if (lotrChar.Contains("dont know") || lotrChar.Contains("no"))
                    {
                        await dc.Prompt("rolePrompt", "Okay let's try a different question: What kind of role do you want to try: Melee, Ranged, Support, or Healer?");
                    }
                    else
                    {
                        await dc.Context.SendActivity(GetLotRCharacter(lotrChar, ref result));
                        if (!result)
                            await dc.Replace("characterBuilder");
                        else
                            await dc.Prompt("rolePrompt", "What role would you like to play?");
                    }
                },
                async(dc, args, next) =>
                {
                    rolePreference = args["Text"].ToString().ToLowerInvariant();

                    await dc.Context.SendActivity(GetClassSuggestionsByRole(rolePreference, ref result));

                    if (!result)
                        await dc.Replace("characterBuilder");
                    else
                        await dc.Prompt("classPrompt", "Of the suggested classes, what sounds fun to you?");
                },
                async(dc, args, next) =>
                {
                    classPreference = args["Text"].ToString().ToLowerInvariant();

                    await dc.Context.SendActivity(GetRaceSuggestions(classPreference, ref result));

                    if (!result)
                        await dc.Replace("characterBuilder");

                    await dc.Prompt("racePrompt", "Of the suggested races, what would you like?");
                },
                async(dc, args, next) =>
                {
                    racePreference = args["Text"].ToString().ToLowerInvariant();

                    await dc.Context.SendActivity(FinalizeSuggestion(racePreference, ref result));

                    if (!result)
                        await dc.Replace("characterBuilder");

                    await dc.Prompt("finalPrompt", "Would you like another suggestion?");
                },
                async(dc, args, next) =>
                {
                    finalResponse = args["Text"].ToString().ToLowerInvariant();

                    if (finalResponse.Contains("yes") || finalResponse.Contains("sure") ||
                        finalResponse.Contains("ok") || finalResponse.Contains("alright"))
                    {
                        await dc.Replace("characterBuilder");
                    }
                    else
                    {
                        await dc.End();
                    }
                }
            });

            // Add a prompt for the character from LotR
            _dialogs.Add("charPrompt", new TextPrompt());
            // Add a prompt for role
            _dialogs.Add("rolePrompt", new TextPrompt());
            // Add a prompt for class
            _dialogs.Add("classPrompt", new TextPrompt());
            // Add a prompt for race
            _dialogs.Add("racePrompt", new TextPrompt());
            // Add a prompt for finalization
            _dialogs.Add("finalPrompt", new TextPrompt());
        }

        public async Task OnTurn(ITurnContext context)
        {
            // This bot is only handling Messages
            if (context.Activity.Type == ActivityTypes.Message)
            {
                var state = context.GetConversationState<DnDState>();
                var dialogContext = _dialogs.CreateContext(context, state);
                await dialogContext.Continue();
                
                // Additional logic can be added to enter each dialog depending on the message received
                string activityText = context.Activity.Text.ToLowerInvariant();

                if (!context.Responded)
                {
                    if (activityText.Contains("recom") || activityText.Contains("class") ||
                        activityText.Contains("race") || activityText.Contains("char") ||
                            activityText.Contains("yes") || activityText.Contains("sure"))
                    {
                        await dialogContext.Begin("characterBuilder");
                    }
                    else
                    {
                        await context.SendActivity($"Hello! Do you want me to suggest a character to play in D&D 5e?");
                    }
                }
            }
        }

        #region Helper Functions
        // Find the information needed to make suggestions based off of LotR character
        public string GetLotRCharacter(string message, ref bool result)
        {
            // Set up in case nothing is found
            string found = "Character not found, lets try again.";
            result = false;

            // Split the message into an array
            var pattern = new Regex(@"\W");
            var words = pattern.Split(message);

            foreach (var word in words)
            {
                foreach (var hero in DnDBuilder.DnDLOTR.Person)
                {
                    if (string.Compare(word.ToLower(), hero.Name.ToLower()) == 0)
                    {
                        // Return what was found
                        found = hero.Name + " could be a [" + hero.SuggestedRoles + "]";
                        result = true;
                        return found;
                    }
                }
            }
            
            return found;
        }

        // Get class suggestions based of role choice
        public string GetClassSuggestionsByRole(string message, ref bool result)
        {
            // Set up in case nothing is found
            string found = "No roles of that were found. Let's start over";
            result = false;

            // Split the message into an array
            var pattern = new Regex(@"\W");
            var words = pattern.Split(message);

            var lotrPref = pattern.Split(lotrChar);
            foreach (var word in lotrPref)
            {
                foreach (var hero in DnDBuilder.DnDLOTR.Person)
                {
                    if (string.Compare(word.ToLower(), hero.Name.ToLower()) == 0)
                    {
                        // Return what was found
                        found = hero.Name + " could be a [" + hero.SuggestedClasses + "]";
                        result = true;
                        return found;
                    }
                }
            }

            // Find the role and set it in the suggestion
            foreach (var word in words)
            {
                foreach (var role in DnDBuilder.DnDRoles.Role)
                {
                    if (string.Compare(word.ToLower(), role.Name.ToLower()) == 0)
                    {
                        found = "The best classes for the role of " + role.Name + " are [" + role.SuggestedClasses + "]";

                        // Return what is found
                        result = true;
                        return found;
                    }
                }
            }

            return found;
        }

        // Set the class and then find suggestions for possible races.
        public string GetRaceSuggestions(string message, ref bool result)
        {
            // Set up in case nothing is found
            string found = "That class wasn't found. Let's start over.";
            string suggestedRaces = "";
            string tempClass = "";
            string[] classAttributes;


            result = false;

            // Split the message into an array
            var pattern = new Regex(@"\W");
            var words = pattern.Split(message);

            // Find the class
            foreach (var word in words)
            {
                foreach (var DnDClass in DnDBuilder.DnDClasses.Class)
                {
                    if (string.Compare(word.ToLower(), DnDClass.Name.ToLower()) == 0)
                    {
                        tempClass = DnDClass.PrimaryAbility;
                        result = true;
                    }
                }
            }

            // Fill the array with the attributes needed. If the class wasn't found, exit.
            if (result)
                classAttributes = tempClass.Split(',').Select(x => x.Trim()).ToArray();
            else
                return found;

            // Find all races and add them to a string, based off class chosen
            foreach (var race in DnDBuilder.DnDRaces.Race)
            {
                if (race.Attributes.Contains(classAttributes[0]))
                {
                    suggestedRaces += race.Name + ", ";
                }
            }

            // Return what is found
            if (result)
            {
                //suggestedRaces = suggestedRaces.Substring(suggestedRaces.Length, suggestedRaces.Length - 3);
                found = "I suggest these races to work best with your chosen class [" + suggestedRaces + "]";
            }

            return found;
        }

        public string FinalizeSuggestion(string message, ref bool result)
        {
            result = false;
            string finalSuggestion = "Given race isn't found. Let's start over.";

            CharacterSuggestion SuggestedChar = new CharacterSuggestion();

            // Set regex to split message
            var pattern = new Regex(@"\W");

            // Find the race and set it
            var racePref = pattern.Split(message);
            foreach (var word in racePref)
            {
                foreach (var race in DnDBuilder.DnDRaces.Race)
                {
                    if (string.Compare(word.ToLower(), race.Name.ToLower()) == 0)
                    {
                        SuggestedChar.DnDRace = race;
                        result = true;
                        break;
                    }
                }
            }

            // If the race wasn't found, exit
            if (!result)
                return finalSuggestion;

            // Find the class and set it
            var classPref = pattern.Split(classPreference);
            foreach (var word in classPref)
            {
                foreach (var DnDClass in DnDBuilder.DnDClasses.Class)
                {
                    if (string.Compare(word.ToLower(), DnDClass.Name.ToLower()) == 0)
                    {
                        SuggestedChar.DnDClass = DnDClass;
                        break;
                    }
                }
            }

            // find the role and set it
            var rolePref = pattern.Split(rolePreference);
            foreach (var word in rolePref)
            {
                foreach (var role in DnDBuilder.DnDRoles.Role)
                {
                    if (string.Compare(word.ToLower(), role.Name.ToLower()) == 0)
                    {
                        SuggestedChar.DnDRole = role;
                        break;
                    }
                }
            }

            // If everything has succeeded thus far
            if (result)
            {
                finalSuggestion = "Based on your preferences I suggest the following:" +
                                "\nRace: " + SuggestedChar.DnDRace.Name +
                                "\nRacial Attributes: " + SuggestedChar.DnDRace.Attributes +
                                "\nRacial Traits: " + SuggestedChar.DnDRace.Traits +
                                "\nClass: " + SuggestedChar.DnDClass.Name +
                                "\nHit Die: " + SuggestedChar.DnDClass.HitDie +
                                "\nPrimary Attribute: " + SuggestedChar.DnDClass.PrimaryAbility +
                                "\nSaves: " + SuggestedChar.DnDClass.Saves +
                                "\n" +
                                "\nRole: " + SuggestedChar.DnDRole.Name;
            }

            return finalSuggestion;
        }
        #endregion
    }
}

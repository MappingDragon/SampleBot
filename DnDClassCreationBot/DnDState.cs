using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using DnDClassCreationBot.Library;

namespace DnDClassCreationBot
{
    /// <summary>
    /// Class for storing conversation state. 
    /// </summary>
    public class DnDState: Dictionary<string, object>
    {
        private const string TurnCountKey = "TurnCount";
        public DnDState()
        {
            this[TurnCountKey] = 0;
        }

        public int TurnCount
        {
            get { return (int)this[TurnCountKey]; }
            set { this[TurnCountKey] = value; }
        }
    }

    #region Data Classes
    [XmlRoot(ElementName = "class")]
    public class Class
    {
        [XmlElement(ElementName = "hitDie")]
        public string HitDie { get; set; }
        [XmlElement(ElementName = "primaryAbility")]
        public string PrimaryAbility { get; set; }
        [XmlElement(ElementName = "saves")]
        public string Saves { get; set; }
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
    }

    [XmlRoot(ElementName = "classes")]
    public class Classes
    {
        [XmlElement(ElementName = "class")]
        public List<Class> Class { get; set; }
    }


    [XmlRoot(ElementName = "race")]
    public class Race
    {
        [XmlElement(ElementName = "attributes")]
        public string Attributes { get; set; }
        [XmlElement(ElementName = "traits")]
        public string Traits { get; set; }
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
    }

    [XmlRoot(ElementName = "races")]
    public class Races
    {
        [XmlElement(ElementName = "race")]
        public List<Race> Race { get; set; }
    }

    [XmlRoot(ElementName = "role")]
    public class Role
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlAttribute(AttributeName = "suggestedclasses")]
        public string SuggestedClasses { get; set; }
    }

    [XmlRoot(ElementName = "roles")]
    public class Roles
    {
        [XmlElement(ElementName = "role")]
        public List<Role> Role { get; set; }
    }

    [XmlRoot(ElementName = "person")]
    public class Person
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlAttribute(AttributeName = "suggestedroles")]
        public string SuggestedRoles { get; set; }
        [XmlAttribute(AttributeName = "suggestedclasses")]
        public string SuggestedClasses { get; set; }
    }

    [XmlRoot(ElementName = "lotrChars")]
    public class LotrChars
    {
        [XmlElement(ElementName = "person")]
        public List<Person> Person { get; set; }
    }

    #endregion

    // Main Builder that will handle the initialization of the data
    public class CharacterBuilder
    {
        // Constructor to immediately load information needed for the bot
        public CharacterBuilder()
        {
            LoadData();
        }

        // Declare the classes we need
        public Classes DnDClasses { get; set; }
        public Races DnDRaces { get; set; }
        public Roles DnDRoles { get; set; }
        public LotrChars DnDLOTR { get; set; }

        private void LoadData()
        {
            DnDClasses = XMLInterpretor.ReadFromXmlFile<Classes>("./Data/Classes.xml");
            DnDRaces = XMLInterpretor.ReadFromXmlFile<Races>("./Data/Races.xml");
            DnDRoles = XMLInterpretor.ReadFromXmlFile<Roles>("./Data/Roles.xml");
            DnDLOTR = XMLInterpretor.ReadFromXmlFile<LotrChars>("./Data/LoTRCharacters.xml");
        }
    }

    public class CharacterSuggestion
    {
        public CharacterSuggestion()
        {
            classSug = "";
            raceSug = "";
            roleSug = "";
        }

        public string classSug { get; set; }
        public string raceSug { get; set; }
        public string roleSug { get; set; }

        public Class DnDClass { get; set; }
        public Race DnDRace { get; set; }
        public Role DnDRole { get; set; }
        public int[] StatArray { get; set; }
    }
}

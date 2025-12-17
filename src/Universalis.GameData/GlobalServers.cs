using System.Collections.Generic;

namespace Universalis.GameData;

/// <summary>
/// Hardcoded mapping of global (non-CN/KR) worlds to data centers.
/// This exists because the upstream Lumina data has a misconfigured data center column offset,
/// causing all worlds to incorrectly map to "Elemental".
/// Data sourced from: https://na.finalfantasyxiv.com/lodestone/worldstatus/
/// </summary>
public static class GlobalServers
{
    /// <summary>
    /// Maps world names to their data center names.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> WorldToDataCenter = new Dictionary<string, string>
    {
        // North America - Aether
        { "Adamantoise", "Aether" },
        { "Cactuar", "Aether" },
        { "Faerie", "Aether" },
        { "Gilgamesh", "Aether" },
        { "Jenova", "Aether" },
        { "Midgardsormr", "Aether" },
        { "Sargatanas", "Aether" },
        { "Siren", "Aether" },

        // North America - Crystal
        { "Balmung", "Crystal" },
        { "Brynhildr", "Crystal" },
        { "Coeurl", "Crystal" },
        { "Diabolos", "Crystal" },
        { "Goblin", "Crystal" },
        { "Malboro", "Crystal" },
        { "Mateus", "Crystal" },
        { "Zalera", "Crystal" },

        // North America - Dynamis
        { "Cuchulainn", "Dynamis" },
        { "Golem", "Dynamis" },
        { "Halicarnassus", "Dynamis" },
        { "Kraken", "Dynamis" },
        { "Maduin", "Dynamis" },
        { "Marilith", "Dynamis" },
        { "Rafflesia", "Dynamis" },
        { "Seraph", "Dynamis" },

        // North America - Primal
        { "Behemoth", "Primal" },
        { "Excalibur", "Primal" },
        { "Exodus", "Primal" },
        { "Famfrit", "Primal" },
        { "Hyperion", "Primal" },
        { "Lamia", "Primal" },
        { "Leviathan", "Primal" },
        { "Ultros", "Primal" },

        // Europe - Chaos
        { "Cerberus", "Chaos" },
        { "Louisoix", "Chaos" },
        { "Moogle", "Chaos" },
        { "Omega", "Chaos" },
        { "Phantom", "Chaos" },
        { "Ragnarok", "Chaos" },
        { "Sagittarius", "Chaos" },
        { "Spriggan", "Chaos" },

        // Europe - Light
        { "Alpha", "Light" },
        { "Lich", "Light" },
        { "Odin", "Light" },
        { "Phoenix", "Light" },
        { "Raiden", "Light" },
        { "Shiva", "Light" },
        { "Twintania", "Light" },
        { "Zodiark", "Light" },

        // Oceania - Materia
        { "Bismarck", "Materia" },
        { "Ravana", "Materia" },
        { "Sephirot", "Materia" },
        { "Sophia", "Materia" },
        { "Zurvan", "Materia" },

        // Japan - Elemental
        { "Aegis", "Elemental" },
        { "Atomos", "Elemental" },
        { "Carbuncle", "Elemental" },
        { "Garuda", "Elemental" },
        { "Gungnir", "Elemental" },
        { "Kujata", "Elemental" },
        { "Tonberry", "Elemental" },
        { "Typhon", "Elemental" },

        // Japan - Gaia
        { "Alexander", "Gaia" },
        { "Bahamut", "Gaia" },
        { "Durandal", "Gaia" },
        { "Fenrir", "Gaia" },
        { "Ifrit", "Gaia" },
        { "Ridill", "Gaia" },
        { "Tiamat", "Gaia" },
        { "Ultima", "Gaia" },

        // Japan - Mana
        { "Anima", "Mana" },
        { "Asura", "Mana" },
        { "Chocobo", "Mana" },
        { "Hades", "Mana" },
        { "Ixion", "Mana" },
        { "Masamune", "Mana" },
        { "Pandaemonium", "Mana" },
        { "Titan", "Mana" },

        // Japan - Meteor
        { "Belias", "Meteor" },
        { "Mandragora", "Meteor" },
        { "Ramuh", "Meteor" },
        { "Shinryu", "Meteor" },
        { "Unicorn", "Meteor" },
        { "Valefor", "Meteor" },
        { "Yojimbo", "Meteor" },
        { "Zeromus", "Meteor" },
    };

}

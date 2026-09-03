namespace Z.GameClients.Stages.Characters.GroundEffects
{
    public enum CharacterGroundEffectType : int
    {
        SummonerRecoverSource,      // heal circle under the caster
        SummonerRecoverTarget,      // heal circle under each summon
        SummonerSummonSource,       // summon circle under the caster
        SummonerSummonTarget,       // summon circle under each summon
        StimulationPackSource,      // attack-speed buff ring under the caster
        StimulationPackTarget,      // attack-speed buff ring under each summon
        ProteinSupplementSource,    // attack-power buff ring under the caster
        ProteinSupplementTarget,    // attack-power buff ring under each summon
    }
}

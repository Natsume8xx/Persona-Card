using Newtonsoft.Json.Linq;
namespace PersonaCards.WebAligned
{
    public static class NativeBattleInput
    {
        // Mirrors whether the native battle controls can be used; the source still validates the action.
        public static bool CanResolve(JObject state, string action) => state != null
            && (action == "play" || action == "discard")
            && (bool?)state["menu"] != true && (bool?)state["inputLocked"] != true
            && string.IsNullOrEmpty((string)state["dialog"])
            && state["selected"] is JArray selected && selected.Count > 0
            && ((int?)state[action == "play" ? "hands" : "discards"] ?? 0) > 0;
    }
}

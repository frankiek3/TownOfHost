using HarmonyLib;
using Hazel;

namespace TownOfHost
{
    //参考:https://github.com/yukieiji/ExtremeRoles/blob/master/ExtremeRoles/Patches/Controller/ExileControllerPatch.cs
    /*[HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    class ExileControllerBeginePatch
    {
        public static void Prefix(ExileController __instance)
        {
            if (Assassin.FinishAssassinMeetingTrigger)
                __instance.completeString = Assassin.ExileText;
        }
    }*/
    class ExileControllerWrapUpPatch
    {
        [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
        class BaseExileControllerPatch
        {
            public static void Postfix(ExileController __instance)
            {
                WrapUpPostfix(__instance.exiled);
            }
        }

        [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
        class AirshipExileControllerPatch
        {
            public static void Postfix(AirshipExileController __instance)
            {
                WrapUpPostfix(__instance.exiled);
            }
        }
        static void WrapUpPostfix(GameData.PlayerInfo exiled)
        {
            Main.witchMeeting = false;
            bool DecidedWinner = false;
            if (!AmongUsClient.Instance.AmHost) return; //ホスト以外はこれ以降の処理を実行しません
            if (Assassin.FinishAssassinMeetingTrigger)
            {
                Utils.GetPlayerById(Assassin.TriggerPlayerId)?.RpcExileV2();
                PlayerState.SetDeathReason(Assassin.TriggerPlayerId, PlayerState.DeathReason.Vote);
                PlayerState.SetDead(Assassin.TriggerPlayerId);
                Utils.GetPlayerById(Assassin.TriggerPlayerId)?.RpcSetNameEx(Assassin.TriggerPlayerName);
                Assassin.FinishAssassinMeetingTrigger = false;
                foreach (var pc in PlayerControl.AllPlayerControls)
                    Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.PlayerSpeedMod;
                Utils.CustomSyncAllSettings();

                if (Assassin.TargetRole == CustomRoles.Marine)
                {
                    AssassinAndMarine.MarineSelectedInAssassinMeeting();
                    AssassinAndMarine.GameEndForAssassinMeeting();
                    return; //インポスター勝利確定なのでこれ以降の処理は不要
                }
            }
            if (exiled != null)
            {
                PlayerState.SetDeathReason(exiled.PlayerId, PlayerState.DeathReason.Vote);
                var role = exiled.GetCustomRole();
                if (role == CustomRoles.Jester && AmongUsClient.Instance.AmHost)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.JesterExiled, Hazel.SendOption.Reliable, -1);
                    writer.Write(exiled.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPC.JesterExiled(exiled.PlayerId);
                    DecidedWinner = true;
                }
                if (role == CustomRoles.Terrorist && AmongUsClient.Instance.AmHost)
                {
                    Utils.CheckTerroristWin(exiled);
                    DecidedWinner = true;
                }
                foreach (var kvp in Main.ExecutionerTarget)
                {
                    var executioner = Utils.GetPlayerById(kvp.Key);
                    if (executioner == null) continue;
                    if (executioner.Data.IsDead || executioner.Data.Disconnected) continue; //Keyが死んでいたらor切断していたらこのforeach内の処理を全部スキップ
                    if (kvp.Value == exiled.PlayerId && AmongUsClient.Instance.AmHost && !DecidedWinner)
                    {
                        //RPC送信開始
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ExecutionerWin, Hazel.SendOption.Reliable, -1);
                        writer.Write(kvp.Key);
                        AmongUsClient.Instance.FinishRpcImmediately(writer); //終了

                        RPC.ExecutionerWin(kvp.Key);
                    }
                }
                if (role != CustomRoles.Witch && Main.SpelledPlayer != null)
                {
                    foreach (var p in Main.SpelledPlayer)
                    {
                        PlayerState.SetDeathReason(p.PlayerId, PlayerState.DeathReason.Spell);
                        Main.IgnoreReportPlayers.Add(p.PlayerId);
                        p.RpcMurderPlayer(p);
                    }
                }
                if (exiled.Object.Is(CustomRoles.TimeThief))
                    exiled.Object.ResetThiefVotingTime();
                if (exiled.Object.Is(CustomRoles.SchrodingerCat) && Options.SchrodingerCatExiledTeamChanges.GetBool())
                    exiled.Object.ExiledSchrodingerCatTeamChange();


                PlayerState.SetDead(exiled.PlayerId);
            }
            if (AmongUsClient.Instance.AmHost && Main.IsFixedCooldown)
                Main.RefixCooldownDelay = Main.RealOptionsData.KillCooldown - 3f;
            Main.SpelledPlayer.RemoveAll(pc => pc == null || pc.Data == null || pc.Data.IsDead || pc.Data.Disconnected);
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                pc.ResetKillCooldown();
                if (Options.MayorHasPortableButton.GetBool() && pc.Is(CustomRoles.Mayor))
                    pc.RpcGuardAndKill();
                if (pc.Is(CustomRoles.Warlock))
                {
                    Main.CursedPlayers[pc.PlayerId] = null;
                    Main.isCurseAndKill[pc.PlayerId] = false;
                }
            }
            if (Assassin.IsAssassinMeeting)
                Assassin.BootAssassinTrigger(Utils.GetPlayerById(Assassin.TriggerPlayerId));
            Utils.CountAliveImpostors();
            Utils.AfterMeetingTasks();
            Utils.CustomSyncAllSettings();
            Utils.NotifyRoles();
            Logger.Info("タスクフェイズ開始", "Phase");
        }
    }
}
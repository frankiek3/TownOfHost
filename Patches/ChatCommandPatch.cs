using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    class ChatCommands
    {
        public static bool Prefix(ChatController __instance)
        {
            var text = __instance.TextArea.text;
            string[] args = text.Split(' ');
            string subArgs = "";
            var canceled = false;
            var cancelVal = "";
            Main.isChatCommand = true;
            Logger.Info(text, "SendChat");
            switch (args[0])
            {
                case "/dump":
                    canceled = true;
                    Utils.DumpLog();
                    break;
                case "/v":
                case "/version":
                    canceled = true;
                    string version_text = "";
                    foreach (var kvp in Main.playerVersion.OrderBy(pair => pair.Key))
                    {
                        version_text += $"{kvp.Key}:{Utils.GetPlayerById(kvp.Key)?.Data?.PlayerName}:{kvp.Value.version}({kvp.Value.tag})\n";
                    }
                    if (version_text != "") HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, version_text);
                    break;
                default:
                    Main.isChatCommand = false;
                    break;
            }
            if (AmongUsClient.Instance.AmHost)
            {
                Main.isChatCommand = true;
                switch (args[0])
                {
                    case "/win":
                    case "/winner":
                        canceled = true;
                        Utils.SendMessage("Winner: " + string.Join(",", Main.winnerList.Select(b => Main.AllPlayerNames[b])));
                        break;

                    case "/l":
                    case "/lastroles":
                        canceled = true;
                        Utils.ShowLastRoles();
                        break;

                    case "/r":
                    case "/rename":
                        canceled = true;
                        if (args.Length > 1) { Main.nickName = args[1]; }
                        break;

                    case "/n":
                    case "/now":
                        canceled = true;
                        Utils.ShowActiveSettings();
                        break;

                    case "/dis":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "crewmate":
                                ShipStatus.Instance.enabled = false;
                                ShipStatus.RpcEndGame(GameOverReason.HumansDisconnect, false);
                                break;

                            case "impostor":
                                ShipStatus.Instance.enabled = false;
                                ShipStatus.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                                break;

                            default:
                                __instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor");
                                cancelVal = "/dis";
                                break;
                        }
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Admin, 0);
                        break;

                    case "/h":
                    case "/help":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "r":
                            case "roles":
                                subArgs = args.Length < 3 ? "" : args[2];
                                GetRolesInfo(subArgs);
                                break;

                            case "att":
                            case "attributes":
                                subArgs = args.Length < 3 ? "" : args[2];
                                switch (subArgs)
                                {
                                    case "lastimpostor":
                                    case "limp":
                                        Utils.SendMessage(GetString("LastImpostor") + GetString("LastImpostorInfo"));
                                        break;

                                    default:
                                        Utils.SendMessage($"{GetString("Command.h_args")}:\n lastimpostor(limp)");
                                        break;
                                }
                                break;

                            case "m":
                            case "modes":
                                subArgs = args.Length < 3 ? "" : args[2];
                                switch (subArgs)
                                {
                                    case "hideandseek":
                                    case "has":
                                        Utils.SendMessage(GetString("HideAndSeekInfo"));
                                        break;

                                    case "nogameend":
                                    case "nge":
                                        Utils.SendMessage(GetString("NoGameEndInfo"));
                                        break;

                                    case "syncbuttonmode":
                                    case "sbm":
                                        Utils.SendMessage(GetString("SyncButtonModeInfo"));
                                        break;

                                    case "randommapsmode":
                                    case "rmm":
                                        Utils.SendMessage(GetString("RandomMapsModeInfo"));
                                        break;

                                    default:
                                        Utils.SendMessage($"{GetString("Command.h_args")}:\n hideandseek(has), nogameend(nge), syncbuttonmode(sbm), randommapsmode(rmm)");
                                        break;
                                }
                                break;


                            case "n":
                            case "now":
                                Utils.ShowActiveRoles();
                                break;

                            default:
                                Utils.ShowHelp();
                                break;
                        }
                        break;

                    default:
                        Main.isChatCommand = false;
                        break;
                }
            }
            if (canceled)
            {
                Logger.Info("Command Canceled", "ChatCommand");
                __instance.TextArea.Clear();
                __instance.TextArea.SetText(cancelVal);
                __instance.quickChatMenu.ResetGlyphs();
            }
            return !canceled;
        }

        public static void GetRolesInfo(string role)
        {
            var roleList = new Dictionary<CustomRoles, string>
            {
                //Impostor役職
                { (CustomRoles)(-1),"== Impostor ==" }, //区切り用
                { CustomRoles.BountyHunter,"bo" },
                { CustomRoles.FireWorks,"fw" },
                { CustomRoles.Mafia,"mf" },
                { CustomRoles.SerialKiller,"sk" },
                { CustomRoles.ShapeMaster,"sha" },
                { CustomRoles.TimeThief,"tt"},
                { CustomRoles.Sniper,"snp" },
                { CustomRoles.Puppeteer,"pup" },
                { CustomRoles.Vampire,"va" },
                { CustomRoles.Warlock,"wa" },
                { CustomRoles.Witch,"wi" },
                //Madmate役職
                { (CustomRoles)(-2),"== Madmate ==" }, //区切り用
                { CustomRoles.MadGuardian,"mg" },
                { CustomRoles.Madmate,"mm" },
                { CustomRoles.MadSnitch,"msn" },
                { CustomRoles.SKMadmate,"sm" },
                //両陣営役職
                { (CustomRoles)(-3),"== Impostor or Crewmate ==" }, //区切り用
                { CustomRoles.Watcher,"wat" },
                //コンビネーション役職
                { (CustomRoles)(-4),"== Combination ==" }, //区切り用
                { CustomRoles.AssassinAndMarine,"aam" },
                //Crewmate役職
                { (CustomRoles)(-5),"== Crewmate ==" }, //区切り用
                { CustomRoles.Bait,"ba" },
                { CustomRoles.Dictator,"dic" },
                { CustomRoles.Doctor,"doc" },
                { CustomRoles.Lighter,"li" },
                { CustomRoles.Mayor,"my" },
                { CustomRoles.SabotageMaster,"sa" },
                { CustomRoles.Sheriff,"sh" },
                { CustomRoles.Snitch,"sn" },
                { CustomRoles.SpeedBooster,"sb" },
                { CustomRoles.Trapper,"tra" },
                //Neutral役職
                { (CustomRoles)(-6),"== Neutral ==" }, //区切り用
                { CustomRoles.Arsonist,"ar" },
                { CustomRoles.Egoist,"eg" },
                { CustomRoles.Executioner,"exe" },
                { CustomRoles.Jester,"je" },
                { CustomRoles.Opportunist,"op" },
                { CustomRoles.SchrodingerCat,"sc" },
                { CustomRoles.Terrorist,"te" },
                //Sub役職
                { (CustomRoles)(-7),"== Sub Role ==" }, //区切り用
                {CustomRoles.Lovers,"lo" },
                //HAS
                { (CustomRoles)(-8),"== Hide and Seek ==" }, //区切り用
                { CustomRoles.HASFox,"hfo" },
                { CustomRoles.HASTroll,"htr" },

            };
            var msg = "";
            var rolemsg = $"{GetString("Command.h_args")}";
            foreach (var r in roleList)
            {
                var roleName = r.Key.ToString();
                var roleShort = r.Value;

                if (String.Compare(role, roleName, true) == 0 || String.Compare(role, roleShort, true) == 0)
                {
                    string RoleName = GetString(roleName);
                    if (r.Key is CustomRoles.AssassinAndMarine)
                        RoleName = AssassinAndMarine.DisplayRole(disableColor: true);
                    Utils.SendMessage(RoleName + GetString($"{roleName}InfoLong"));
                    return;
                }

                var roleText = $"{roleName.ToLower()}({roleShort.ToLower()}), ";
                if ((int)r.Key < 0)
                {
                    msg += rolemsg + "\n" + roleShort + "\n";
                    rolemsg = "";
                }
                else if ((rolemsg.Length + roleText.Length) > 40)
                {
                    msg += rolemsg + "\n";
                    rolemsg = roleText;
                }
                else
                {
                    rolemsg += roleText;
                }
            }
            msg += rolemsg;
            Utils.SendMessage(msg);
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    class ChatUpdatePatch
    {
        public static void Postfix(ChatController __instance)
        {
            if (!AmongUsClient.Instance.AmHost || Main.MessagesToSend.Count < 1) return;
            (string msg, byte sendTo) = Main.MessagesToSend[0];
            Main.MessagesToSend.RemoveAt(0);
            int clientId = sendTo == byte.MaxValue ? -1 : sendTo;
            if (clientId == -1) DestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, msg);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendChat, SendOption.None, clientId);
            writer.Write(msg);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    class AddChatPatch
    {
        public static void Postfix(ref PlayerControl sourcePlayer, ref string chatText)
        {
            Logger.Info($"送信者：{sourcePlayer.Data.PlayerName}, テキスト：{chatText.TrimEnd()}", "AddChat");
            foreach (var target in PlayerControl.AllPlayerControls)
            {
                if (!Assassin.IsAssassinMeeting) continue;
                Logger.Info($"{Utils.GetPlayerById(Assassin.TriggerPlayerId).Data.PlayerName}({Assassin.TriggerPlayerId})と{sourcePlayer.Data.PlayerName}({sourcePlayer.PlayerId})が同じ : {Assassin.TriggerPlayerId == sourcePlayer.PlayerId}", "AssassinMeeting");
                Logger.Info($"{chatText.TrimEnd()}と{target.Data.PlayerName}が同じ : {chatText.TrimEnd() == target.Data.PlayerName}", "AssassinMeeting");
                if (Assassin.TriggerPlayerId == sourcePlayer.PlayerId && chatText.TrimEnd() == target.Data.PlayerName)
                {
                    Assassin.AssassinTargetId = target.PlayerId;
                    Assassin.TargetRole = target.GetCustomRole();
                    Assassin.FinishAssassinMeetingTrigger = true;
                    Logger.Info($"アサシン会議終了...対象の役職 : {Assassin.TargetRole}", "Assassin");
                    break;
                }
            }

            switch (chatText)
            {
                case "/kill":
                    sourcePlayer.RpcMurderPlayer(sourcePlayer);
                    break;
                default:
                    break;
            }
            if (!AmongUsClient.Instance.AmHost) return;
        }
    }
}
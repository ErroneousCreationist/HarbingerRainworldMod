//using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using System.Collections.Generic;
using MoreSlugcats;
using RWCustom;
using System.Runtime.CompilerServices;
using SlugBase.SaveData;
using IL.Menu;
using Mono.Cecil.Cil;
using MonoMod;
using MonoMod.Cil;
using CreatureType = CreatureTemplate.Type;

namespace Harbinger
{
    class HarbingerEntryTutorial : UpdatableAndDeletable
    {
        private float currmessage = 0;
        public HarbingerEntryTutorial(Room room)
        {
            this.room = room;
            this.currmessage = 0;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (this.room.game.session.Players[0].realizedCreature == null || this.room.game.session.Players[0].realizedCreature.room != room || this.room.game.cameras[0].hud == null || this.room.game.cameras[0].hud.textPrompt == null || this.room.game.cameras[0].hud.textPrompt.messages.Count >= 1)
            {
                return;
            }
            while (currmessage < 3)
            {
                if (currmessage == 0)
                {
                    this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.rainWorld.inGameTranslator.Translate("You gain more food pips from eating insect-type creatures."), 120, 460, true, true);
                }
                else if (currmessage == 1)
                {
                    this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.rainWorld.inGameTranslator.Translate("Electric creatures charge you up, allowing you to electrocute stunned creatures by holding grab."), 20, 260, true, true);
                }
                else if (currmessage == 2)
                {
                    this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.rainWorld.inGameTranslator.Translate("Your stored electricity can also free you from grasps, and charge thrown spears."), 20, 210, true, true);
                }
                else
                {
                    this.Destroy();
                }
                currmessage++;
            }

        }
    }

    class HarbingerMushroomTutorial : UpdatableAndDeletable
    {
        private float currmessage = 0;
        public HarbingerMushroomTutorial(Room room)
        {
            this.room = room;
            this.currmessage = 0;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (this.room.game.session.Players[0].realizedCreature == null || this.room.game.session.Players[0].realizedCreature.room != room || this.room.game.cameras[0].hud == null || this.room.game.cameras[0].hud.textPrompt == null || this.room.game.cameras[0].hud.textPrompt.messages.Count >= 1)
            {
                return;
            }
            while (currmessage < 3)
            {
                if (currmessage == 0)
                {
                    this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.rainWorld.inGameTranslator.Translate("At the cost of a food pip, press up and grab while airbourne to enter an adrenaline state."), 20, 260, true, true);
                }
                else if (currmessage == 1)
                {
                    this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.rainWorld.inGameTranslator.Translate("Chucking spears in this state does extra damage and flings you further."), 20, 220, true, true);
                }
                else if(currmessage==2)
                {
                    this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.rainWorld.inGameTranslator.Translate("You can hold down while on the ground to cancel the state."), 20, 200, true, true);
                }
                else
                {
                    this.Destroy();
                }
                currmessage++;
            }
        }
    }

    [BepInPlugin(MOD_ID, "Harbinger", "0.1.0")]

    class Plugin : BaseUnityPlugin
    {
        //add tutorial
        //this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.manager.rainWorld.inGameTranslator.Translate("Combat and other fast movements will quickly exhaust you."), 120, 160, false, true);
        public class HarbingerData
        {
            public float Charge, DischargeFac, DisableEffectsFac, ElectromaulT;

            public HarbingerData(float charge, float dischargeFac)
            {
                Charge = charge;
                DischargeFac = dischargeFac;
                DisableEffectsFac = 0;
                ElectromaulT = 0;
            }
        }

        public class SpearData
        {
            public float Charged = 0;
            public SpearData (float charged)
            {
                Charged = charged;
            }
        }

        private const string MOD_ID = "erroneous.harbinger";

        public static readonly GameFeature<bool> HarbingerDialogue = GameBool("Harbinger/pebbsidialogue");
        public static readonly PlayerFeature<bool> InedibleMushrooms = PlayerBool("Harbinger/inediblemushrooms");
        public static readonly PlayerFeature<int> MushroomTime = PlayerInt("Harbinger/mushroomtime");
        public static readonly PlayerFeature<bool> ExhaustionImmunity = PlayerBool("Harbinger/exhaustionimmune");
        public static readonly PlayerFeature<float> StarvingDmgBonus = PlayerFloat("Harbinger/dmgbonus_starve");
        public static readonly PlayerFeature<float> MushroomDamageBonus = PlayerFloat("Harbinger/dmgbonus_mush");
        public static readonly PlayerFeature<float> DischargeTime = PlayerFloat("Harbinger/dischargetime");
        public static readonly PlayerFeature<float> FrameSparkChance = PlayerFloat("Harbinger/sparkframechance");
        public static readonly PlayerFeature<float> SpearStunMult = PlayerFloat("Harbinger/spearstunmult");

        public static ConditionalWeakTable<Player, HarbingerData> PLAYER_VALUES = new ConditionalWeakTable<Player, HarbingerData>();
        public static ConditionalWeakTable<Spear, SpearData> SPEAR_CHARGE = new ConditionalWeakTable<Spear, SpearData>();
       
        // Add hooks
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            #region Player Hooks
            On.Player.ObjectEaten += Player_Eat;
            On.Player.Update += Player_Update;
            On.SSOracleBehavior.Update += Pebbsi_Update;
            On.Player.ThrownSpear += Player_ThrowSpear;
            On.Creature.Grab += Creature_OnGrab;
            On.Player.InitiateGraphicsModule += Player_AddToWeakTable;
            On.Player.EatMeatUpdate += Player_EatCorpse;
            On.Player.SlugcatGrab += Player_SlugcatGrab;
            On.Player.GrabUpdate += Player_ElectricGrab;
            On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConvoOverride;
            On.SaveState.GetStoryDenPosition += StartDenOverride;
            On.PlayerGraphics.Gown.DrawSprite += Gown_DrawSprite;
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.Gown.Update += Gown_Update;
            On.PlayerGraphics.Gown.ApplyPalette += Gown_ApplyPalette;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            On.Spear.HitSomething += Spear_HitSomething;
            On.Spear.Update += Spear_Update;
            On.RoomSpecificScript.AddRoomSpecificScript += RoomSpecificScript_AddRoomSpecificScript;
            On.Player.Grabability += Player_Grabability;
            IL.ScavengerTreasury.ctor += ScavengerTreasury_ctor;
            #endregion

        }

        #region main harbinger voids
        private void ScavengerTreasury_ctor(MonoMod.Cil.ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After,
                x => x.MatchLdarg(1),
                x => x.MatchLdfld<World>("world"),
                x => x.MatchLdfld<Region>("region"),
                x => x.MatchLdfld<string>("name"),
                x => x.MatchLdstr("LC"),
                x => x.MatchCallvirt<System.String>("op_Equality")
                ) ;
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate((bool orig, Room room) => { return orig || ( room.world.region!=null && room.world.region.name == "EZ"); });
            Debug.Log(il.ToString() + " ZESTY IL EDITS!!!");
        }


        //allow harb to grab stunned creatures
        private Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            Player.ObjectGrabability b = orig(self, obj);
            if (obj is Creature && !(obj as Creature).Template.smallCreature && ((obj as Creature).dead || (self.SlugCatClass.ToString() == "Erroneous.Harbinger" && self.dontGrabStuff < 1 && obj != self && !(obj as Creature).Consious)))
            {
                return Player.ObjectGrabability.Drag;
            }
            return b;
        }

        private void RoomSpecificScript_AddRoomSpecificScript(On.RoomSpecificScript.orig_AddRoomSpecificScript orig, Room room)
        {
            orig(room);

            if(room.abstractRoom.name == "EZ_harbstart")
            {
                if (room.game.session is StoryGameSession && (room.game.session as StoryGameSession).saveState.cycleNumber == 0)
                {
                    room.AddObject(new HarbingerEntryTutorial(room));

                }
            }
            if (room.abstractRoom.name == "EZ_B01")
            {
                if (room.game.session is StoryGameSession && (room.game.session as StoryGameSession).saveState.cycleNumber ==0)
                {
                    room.AddObject(new HarbingerMushroomTutorial(room));
                }
            }
        }

        private void Spear_Update(On.Spear.orig_Update orig, Spear self, bool eu)
        {
            orig(self, eu);
            if(SPEAR_CHARGE.TryGetValue(self, out SpearData data))
            {
                if (Random.value < 0.2f * (Mathf.LerpUnclamped(0.3f, 1.5f, data.Charged)))
                {
                    self.room.AddObject(new Spark(self.firstChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                }

                if(data.Charged==0)
                {
                    SPEAR_CHARGE.Remove(self);
                }
            }
        }

        private bool Spear_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            bool b = orig(self, result, eu);
            if(SPEAR_CHARGE.TryGetValue(self,out SpearData data))
            {
                if(result.obj is Creature)
                {
                    //if our spear just bounced off, we shouldn't shock the creature
                        //self.room.AddObject(new CreatureSpasmer(result.obj as Creature, true, 120));
                    bool flag = result.obj is Centipede || result.obj is BigJellyFish || result.obj is Inspector;
                    if (!(result.obj is BigEel) && !flag && b)
                    {
                        float mult = 3100;
                        bool yes = self.thrownBy is Player ? SpearStunMult.TryGet((self.thrownBy as Player), out mult) : false;
                        float stunpower = Custom.LerpMap(result.obj.TotalMass, 0f, data.Charged * 9.8f, 300f, 30f);
                        Debug.Log("WE STUNNED FOR "+stunpower.ToString()+" FRAMES (FRUITY)");
                        (result.obj as Creature).Violence(self.firstChunk, new Vector2?(Custom.DirVec(self.firstChunk.pos, (result.obj).firstChunk.pos) * 5f), result.obj.firstChunk, null, Creature.DamageType.Electric, 0.1f, stunpower);
                        self.room.AddObject(new CreatureSpasmer(result.obj as Creature, false, (int)((result.obj as Creature).stun)));
                        self.room.PlaySound(SoundID.Centipede_Shock, self.firstChunk.pos);
                    }
                    if (self.Submersion <= 0.5f && (result.obj).Submersion > 0.5f)
                    {
                        self.room.AddObject(new UnderwaterShock(self.room, null, (result.obj).firstChunk.pos, 10, 800f, 2f, self.thrownBy, new Color(0.8f, 0.8f, 1f)));
                    }
                    SPEAR_CHARGE.Remove(self);
                }
            }
            return b;
        }

        private void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            //make glow somehow idk
        }

        private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if(self.player.SlugCatClass.ToString() == "Erroneous.Harbinger")
            {
                if (self.player.room != null)
                {
                    int index = sLeaser.sprites.Length - 2;
                    sLeaser.sprites[index].rotation = sLeaser.sprites[9].rotation;
                    if (self.player.animation == Player.AnimationIndex.Flip)
                    {
                        Vector2 vector13 = Custom.DegToVec(sLeaser.sprites[9].rotation) * 4f;
                        sLeaser.sprites[index].x = sLeaser.sprites[9].x + vector13.x;
                        sLeaser.sprites[index].y = sLeaser.sprites[9].y + vector13.y;
                    }
                    else
                    {
                        int num10 = 0;
                        string name = sLeaser.sprites[9].element.name;
                        if (name[name.Length - 2] == 'C')
                        {
                            num10 = int.Parse(name[name.Length - 1].ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                            sLeaser.sprites[index].x = sLeaser.sprites[9].x + 3f + 4f * ((float)num10 / 8f);
                        }
                        else if (name[name.Length - 2] == 'D')
                        {
                            num10 = int.Parse(name[name.Length - 1].ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                            sLeaser.sprites[index].x = sLeaser.sprites[9].x + 3f * (1f - (float)num10 / 8f);
                        }
                        else
                        {
                            sLeaser.sprites[index].x = sLeaser.sprites[9].x * (1f - (float)num10 / 8f);
                        }
                        sLeaser.sprites[index].scaleX = 0.23f;
                        sLeaser.sprites[index].scaleY = self.blink > 0 ? 0.1f : 0.35f;
                        sLeaser.sprites[index].color = Color.white;
                        sLeaser.sprites[index].y = sLeaser.sprites[9].y + 4.5f;
                    }

                    int markindex = sLeaser.sprites.Length - 1;
                    Vector2 vector2 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
                    Vector2 vector3 = Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker);
                    Vector2 markpos = vector3 + Custom.DirVec(vector2, vector3) * 30f + new Vector2(0f, 30f);
                    sLeaser.sprites[markindex].color = Color.white;
                    sLeaser.sprites[markindex].x = markpos.x - camPos.x;
                    sLeaser.sprites[markindex].y = markpos.y - camPos.y;
                    sLeaser.sprites[markindex].alpha = 0.6f * Mathf.Lerp(self.lastMarkAlpha, self.markAlpha, timeStacker);
                    //sLeaser.sprites[markindex].scale = 1f + Mathf.Lerp(this.lastMarkAlpha, this.markAlpha, timeStacker);
                }
            }
        }

        private void Gown_ApplyPalette(On.PlayerGraphics.Gown.orig_ApplyPalette orig, PlayerGraphics.Gown self, int sprite, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (self.owner.player.SlugCatClass.ToString() == "Erroneous.Harbinger")
            {
                ColorUtility.TryParseHtmlString("#0e0624", out Color col);
                for (int i = 0; i < self.divs; i++)
                {
                    for (int j = 0; j < self.divs; j++)
                    {
                        (sLeaser.sprites[sprite] as TriangleMesh).verticeColors[j * self.divs + i] = col;
                    }
                }
                return;
            }
            orig(self, sprite, sLeaser, rCam, palette);
        }

        private void Gown_Update(On.PlayerGraphics.Gown.orig_Update orig, PlayerGraphics.Gown self)
        {
            if (self.owner.player.SlugCatClass.ToString() == "Erroneous.Harbinger")
            {
                self.visible = true;
                ColorUtility.TryParseHtmlString("#0e0624", out Color col);
            }
            orig(self);
        }

        private void Gown_DrawSprite(On.PlayerGraphics.Gown.orig_DrawSprite orig, PlayerGraphics.Gown self, int sprite, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if(self.owner.player.SlugCatClass.ToString() == "Erroneous.Harbinger")
            {
                self.visible = true;
            }
            orig(self, sprite, sLeaser, rCam, timeStacker, camPos);
        }

        private void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if(self.player.SlugCatClass.ToString() == "Erroneous.Harbinger")
            {
                //this doesnt work it just removes the existing mark lol
                sLeaser.sprites[11].RemoveFromContainer();
                sLeaser.sprites[11] = new FSprite("atlases/harbingerhalo");
                sLeaser.sprites[11].shader = rCam.game.rainWorld.Shaders["FlatLight"];
                //add our face thibngy
                int facemarkindex = sLeaser.sprites.Length;
                System.Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
                sLeaser.sprites[facemarkindex] = new FSprite("Pebble1", true);
                sLeaser.sprites[facemarkindex].alpha = 1f;
                sLeaser.sprites[facemarkindex].color = Color.white;

                int markindex = sLeaser.sprites.Length;
                System.Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
                sLeaser.sprites[markindex] = new FSprite("atlases/harbingerhalo",true);
                sLeaser.sprites[markindex].shader = rCam.game.rainWorld.Shaders["FlatLight"];
                sLeaser.sprites[markindex].scaleX = 0.15f;
                sLeaser.sprites[markindex].scaleY = 0.15f;
                sLeaser.sprites[facemarkindex].color = Color.white;
                self.AddToContainer(sLeaser, rCam, null);
            }
        }

        private string StartDenOverride(On.SaveState.orig_GetStoryDenPosition orig, SlugcatStats.Name slugcat, out bool isVanilla)
        {
            string val = orig(slugcat, out isVanilla);
            if(slugcat.ToString() == "Erroneous.Harbinger")
            {
                isVanilla = false;
                return "EZ_harbstart";
            }

            return val;
        }

        private void PebblesConvoOverride(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
        {
            if(HarbingerDialogue.TryGet(self.owner.oracle.room.game, out bool custom) && custom)
            {
                self.events = new List<Conversation.DialogueEvent>
                {
                    new Conversation.TextEvent(self, 0, self.Translate("Oh."), 2),
                    new Conversation.TextEvent(self, 0, self.Translate("Of course."), 2),
                    new Conversation.TextEvent(self, 0, self.Translate("I was expecting you."), 2),
                    new Conversation.TextEvent(self, 0, self.Translate("The omen, the bringer of unfortunate news."), 4),
                    new Conversation.TextEvent(self, 0, self.Translate("The Harbinger."), 3),
                    new Conversation.TextEvent(self, 0, self.Translate("However... you appear to be late."), 3),
                    new Conversation.TextEvent(self, 0, self.Translate("She has already collapsed, and I have my issue under control."), 4),
                    new Conversation.TextEvent(self, 0, self.Translate("You appear to have an... encoded message. Stored in your very being."), 4),
                    new Conversation.TextEvent(self, 0, self.Translate("Allow me to read it..."), 2),
                    new Conversation.SpecialEvent(self, 0, "karma"),
                    new Conversation.TextEvent(self, 3, self.Translate("'On behalf of iterator Distant Sands of Time, this message has been sent... \n incoming circumstances... unavoidable...'"), 5),
                    new Conversation.TextEvent(self, 0, self.Translate("..."), 2),
                    new Conversation.TextEvent(self, 0, self.Translate("Thats enough. Do you really think your 'charity' is really helping anybody?"), 2),
                    new Conversation.TextEvent(self, 0, self.Translate("I wanted to find the solution MYSELF, and that is what I will do."), 2),
                    new Conversation.TextEvent(self, 0, self.Translate("I don't need to be told what to do and what not to do to 'avert these events'"), 2),
                    new Conversation.TextEvent(self, 0, self.Translate("I... I have... work to do."), 2),
                    new Conversation.TextEvent(self, 0, self.Translate("You may leave now..."), 2),
                };
            }
        }

        private bool CanMaulCreature(Player self, Creature crit)
        {
            bool flag = true;
            if (ModManager.CoopAvailable)
            {
                Player player = crit as Player;
                if (player != null && (player.isNPC || !Custom.rainWorld.options.friendlyFire))
                {
                    flag = false;
                }
            }

            return crit is not Fly
                && !crit.dead
                //&& !(crit is IPlayerEdible)
                && !(crit is Centipede)
                && flag;
                //&& self.IsCreatureLegalToHoldWithoutStun(crit);
        }

        private void Harbinger_MaulingUpdate(Player self, int graspIndex, float t)
        {
            if (self.grasps[graspIndex] == null || !(self.grasps[graspIndex].grabbed is Creature))
            {
                return;
            }
            if (t > 0.25f)
            {
                if ((self.grasps[graspIndex].grabbed as Creature).abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Scavenger)
                {
                    self.grasps[graspIndex].grabbed.bodyChunks[0].mass = 0.5f;
                    self.grasps[graspIndex].grabbed.bodyChunks[1].mass = 0.3f;
                    self.grasps[graspIndex].grabbed.bodyChunks[2].mass = 0.05f;
                }
                self.standing = false;
                self.Blink(5);
                if (self.maulTimer % 3 == 0)
                {
                    Vector2 b = Custom.RNV() * 3f;
                    self.mainBodyChunk.pos += b;
                    self.mainBodyChunk.vel += b;
                }
                Vector2 vector = self.grasps[graspIndex].grabbedChunk.pos * self.grasps[graspIndex].grabbedChunk.mass;
                float num = self.grasps[graspIndex].grabbedChunk.mass;
                for (int i = 0; i < self.grasps[graspIndex].grabbed.bodyChunkConnections.Length; i++)
                {
                    if (self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk1 == self.grasps[graspIndex].grabbedChunk)
                    {
                        vector += self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk2.pos * self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk2.mass;
                        num += self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk2.mass;
                    }
                    else if (self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk2 == self.grasps[graspIndex].grabbedChunk)
                    {
                        vector += self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk1.pos * self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk1.mass;
                        num += self.grasps[graspIndex].grabbed.bodyChunkConnections[i].chunk1.mass;
                    }
                }
                vector /= num;
                self.mainBodyChunk.vel += Custom.DirVec(self.mainBodyChunk.pos, vector) * 0.5f;
                self.bodyChunks[1].vel -= Custom.DirVec(self.mainBodyChunk.pos, vector) * 0.6f;
                if (self.graphicsModule != null)
                {
                    if (!Custom.DistLess(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos, self.grasps[graspIndex].grabbedChunk.rad))
                    {
                        (self.graphicsModule as PlayerGraphics).head.vel += Custom.DirVec(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos) * (self.grasps[graspIndex].grabbedChunk.rad - Vector2.Distance(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos));
                    }
                    else if (self.maulTimer % 5 == 3)
                    {
                        (self.graphicsModule as PlayerGraphics).head.vel += Custom.RNV() * 4f;
                    }
                    if (self.maulTimer > 10 && self.maulTimer % 8 == 3)
                    {
                        self.mainBodyChunk.pos += Custom.DegToVec(Mathf.Lerp(-90f, 90f, Random.value)) * 4f;
                        self.grasps[graspIndex].grabbedChunk.vel += Custom.DirVec(vector, self.mainBodyChunk.pos) * 0.9f / self.grasps[graspIndex].grabbedChunk.mass;
                        for (int j = Random.Range(0, 3); j >= 0; j--)
                        {
                            self.room.AddObject(new Spark(self.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                        }
                        return;
                    }
                }
            }
        }

        private void Harbinger_Grab(Player self, bool eu)
        {
            if (self.SlugCatClass.ToString() == "Erroneous.Harbinger")
            {
                int num11 = 0;
                if (ModManager.MMF && (self.grasps[0] == null || !(self.grasps[0].grabbed is Creature)) && self.grasps[1] != null && self.grasps[1].grabbed is Creature)
                {
                    num11 = 1;
                }

                bool yes = PLAYER_VALUES.TryGetValue(self, out HarbingerData data);

                bool b = self.input[0].pckp &&
                    self.grasps[num11] != null &&
                    self.grasps[num11].grabbed is Creature &&
                    (CanMaulCreature(self, (self.grasps[num11].grabbed as Creature)) || self.maulTimer > 1) &&
                    yes &&
                    !(self.grasps[num11].grabbed as Creature).dead &&
                    (self.grasps[num11].grabbed as Creature) is not Centipede &&
                    data.Charge > 0;
               
                if (b)
                {
                    Debug.Log("WE ARE MAULING " + data.ElectromaulT.ToString());
                    data.ElectromaulT += Time.deltaTime;
                    (self.grasps[num11].grabbed as Creature).Stun(60);
                    Harbinger_MaulingUpdate(self, num11, data.ElectromaulT);
                    if (self.spearOnBack != null)
                    {
                        self.spearOnBack.increment = false;
                        self.spearOnBack.interactionLocked = true;
                    }
                    if (self.slugOnBack != null)
                    {
                        self.slugOnBack.increment = false;
                        self.slugOnBack.interactionLocked = true;
                    }
                    if (self.grasps[num11] != null && data.ElectromaulT>1.4f)
                    {
                        Debug.Log("WE FINISHED ELECTRIC MAUL");
                        self.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, self.mainBodyChunk);
                        //self.room.PlaySound(SoundID.Drop_Bug_Grab_Creature, self.mainBodyChunk, false, 1f, 0.76f);
                        if (RainWorld.ShowLogs)
                        {
                            Debug.Log("Shocked target");
                        }
                        if (!(self.grasps[num11].grabbed as Creature).dead)
                        {
                            for (int num12 = Random.Range(8, 14); num12 >= 0; num12--)
                            {
                                self.room.AddObject(new Spark(self.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                            }
                            Creature creature = self.grasps[num11].grabbed as Creature;
                            ShockTarget(self, creature);
                        }
                        data.ElectromaulT = 0;
                        self.wantToPickUp = 0;
                        if (self.grasps[num11] != null)
                        {
                            self.TossObject(num11, eu);
                            self.ReleaseGrasp(num11);
                        }
                        self.standing = true;
                    }
                    PLAYER_VALUES.Remove(self);
                    PLAYER_VALUES.Add(self, data);
                    return;
                }
                else
                {
                    data.ElectromaulT = 0;
                    PLAYER_VALUES.Remove(self);
                    PLAYER_VALUES.Add(self, data);
                }

                if (self.grasps[num11] != null && self.grasps[num11].grabbed is Creature && (self.grasps[num11].grabbed as Creature).Consious && !self.IsCreatureLegalToHoldWithoutStun(self.grasps[num11].grabbed as Creature))
                {
                    if (RainWorld.ShowLogs)
                    {
                        Debug.Log("Lost hold of live mauling target");
                    }
                    data.ElectromaulT = 0;
                    PLAYER_VALUES.Remove(self);
                    PLAYER_VALUES.Add(self, data);
                    self.wantToPickUp = 0;
                    self.ReleaseGrasp(num11);
                    return;
                }
            }
        }

        private void Player_ElectricGrab(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            if(self.SlugCatClass.ToString() == "Erroneous.Harbinger")
            {
                Harbinger_Grab(self, eu);
            }
            orig(self, eu);
        }

        private void Player_SlugcatGrab(On.Player.orig_SlugcatGrab orig, Player self, PhysicalObject obj, int graspUsed)
        {
            orig(self, obj, graspUsed);
            if(self.SlugCatClass.ToString() == "Erroneous.Harbinger")
            {
                bool yes = PLAYER_VALUES.TryGetValue(self, out HarbingerData data);
                if (ModManager.MSC && obj is ElectricSpear && (obj as ElectricSpear).abstractSpear.electricCharge <= 0 && yes && data.Charge > 0)
                {
                    (obj as ElectricSpear).Recharge();
                }
            }
        }

        private void Player_EatCorpse(On.Player.orig_EatMeatUpdate orig, Player self, int graspIndex)
        {
            orig(self, graspIndex);
            if(self.SlugCatClass.ToString() != "Erroneous.Harbinger") { return; } //only Harb can do this
            if (self.eatMeat>20 && self.graphicsModule != null && (self.grasps[graspIndex].grabbed as Creature).State.meatLeft > 0 && self.FoodInStomach < self.MaxFoodInStomach)
            {
                //if its a centipede
                if (self.grasps[graspIndex].grabbed is Centipede)
                {
                    float mass = (self.grasps[graspIndex].grabbed as Centipede).TotalMass;
                    if (PLAYER_VALUES.TryGetValue(self, out HarbingerData data))
                    {
                        if (mass > data.Charge)
                        {
                            data.Charge = mass;
                            for (int k = 0; k < 20; k++)
                            {
                                self.room.AddObject(new Spark(self.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                            }
                            self.room.AddObject(new ZapCoil.ZapFlash(self.mainBodyChunk.pos, 2.5f));
                            self.room.PlaySound(SoundID.Centipede_Shock, self.mainBodyChunk.pos);
                        }
                        PLAYER_VALUES.Remove(self);
                        PLAYER_VALUES.Add(self, data);
                    }
                }
            }
        }

        private void Player_AddToWeakTable(On.Player.orig_InitiateGraphicsModule orig, Player self)
        {
            orig(self);
            if(self.SlugCatClass.ToString() == "Erroneous.Harbinger")
            {
                float charge = 0;
                
                PLAYER_VALUES.Add(self, new HarbingerData(charge,0)); //add here bc idk i cant find any other start function
            }
        }

        private bool Creature_OnGrab(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying)
        {
            bool ret = orig(self, obj, graspUsed, chunkGrabbed, shareability, dominance, overrideEquallyDominant, pacifying);
            if (obj is Player)
            {
                OnPlayerIsGrabbed(self, obj);
            }
            return ret;

        }

        private void OnPlayerIsGrabbed(Creature grabber, PhysicalObject obj)
        {
            if (PLAYER_VALUES.TryGetValue(obj as Player, out HarbingerData data))
            {
                DischargeTime.TryGet(obj as Player, out float t);
                data.DischargeFac = t;
                PLAYER_VALUES.Remove(obj as Player);
                PLAYER_VALUES.Add(obj as Player, data);
            }
        }

        private void Player_ThrowSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig(self, spear);
            if(self.SlugCatClass.ToString() == "Erroneous.Harbinger")
            {
                //StarvingDmgBonus.TryGet(self, out float starvebonus);
                MushroomDamageBonus.TryGet(self, out float mushbonus);
                //damage bonuses
                spear.spearDamageBonus += (self.mushroomCounter>0 ? mushbonus : 0);

                bool yes = PLAYER_VALUES.TryGetValue(self, out HarbingerData data);
                bool spearyes = SPEAR_CHARGE.TryGetValue(spear, out SpearData chargedata);

                //add a ton of force to us if we are mushroomed
                if(self.mushroomCounter>0)
                {
                    if (self.canJump != 0)
                    {
                        self.animation = Player.AnimationIndex.Roll;
                    }
                    else
                    {
                        self.animation = Player.AnimationIndex.Flip;
                    }
                    if ((self.room != null && self.room.gravity == 0f) || Mathf.Abs(spear.firstChunk.vel.x) < 1f)
                    {
                        self.firstChunk.vel += spear.firstChunk.vel.normalized * 15f;
                    }
                    else
                    {
                        self.rollDirection = (int)Mathf.Sign(spear.firstChunk.vel.x);
                        self.rollCounter = 0;
                        BodyChunk firstChunk3 = self.firstChunk;
                        firstChunk3.vel.x = firstChunk3.vel.x + Mathf.Sign(spear.firstChunk.vel.x) * 15f;
                    }
                }
                else if (yes && !spearyes && data.Charge>0 && spear is not ExplosiveSpear && spear is not ElectricSpear)
                {
                    float power = data.Charge * 0.25f;
                    data.Charge = data.Charge * 0.75f;
                    if(data.Charge<=0.05f)
                    {
                        data.Charge = 0;
                    }
                    SPEAR_CHARGE.Add(spear, new SpearData(power));
                    self.room.AddObject(new ZapCoil.ZapFlash(self.mainBodyChunk.pos, 2f));
                    self.room.PlaySound(SoundID.Centipede_Shock, self.mainBodyChunk.pos, 0.25f, 0.9f);
                }
            }
        }

        //make pebsi unable to harm the Harbinger
        private void Pebbsi_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            orig(self, eu);

            if(self.player!=null && self.player.SlugCatClass.ToString() == "Erroneous.Harbinger" && self.killFac > 0.9f) { 
                self.killFac = 0;
                string[] quotes = { "Why won't you-", "Why won't this-", "How-", "What...?" };
                self.dialogBox.Interrupt(self.Translate(quotes[UnityEngine.Random.Range(0,quotes.Length)]), 0);
                self.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
                self.player.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, self.player.mainBodyChunk, false, 1f, 1f);
                self.player.room.AddObject(new ShockWave(self.player.bodyChunks[0].pos, 100f, 0.07f, 6, false));
            }
        }

        private void ShockTarget(Player self, PhysicalObject shockObj)
        {
            if(self.SlugCatClass.ToString() != "Erroneous.Harbinger") { return; }
            if (self.dead) { return; } //no dead bodies can shock
            bool yes = PLAYER_VALUES.TryGetValue(self, out HarbingerData data);
            float chargepower = data.Charge;
            if(chargepower<= 0) { return; }

            self.room.AddObject(new ZapCoil.ZapFlash(self.mainBodyChunk.pos, 10f));
            self.room.PlaySound(SoundID.Centipede_Shock, self.mainBodyChunk.pos);

            if (shockObj is Creature)
            {
                if (shockObj.TotalMass < chargepower)
                {
                    if (ModManager.MSC && shockObj is Player && (shockObj as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                    {
                        (shockObj as Player).PyroDeath();
                    }
                    else
                    {
                        (shockObj as Creature).Die();
                        self.room.AddObject(new CreatureSpasmer(shockObj as Creature, true, (int)Random.Range(70, 120)));
                    }
                }
                else
                {
                    int power = (int)Custom.LerpMap(shockObj.TotalMass, 0f, data.Charge * 8.8f, 300f, 30f);
                    Debug.Log("SHOCKED WITH: " + power.ToString());
                    (shockObj as Creature).Stun(power);
                    self.room.AddObject(new CreatureSpasmer(shockObj as Creature, false, (shockObj as Creature).stun));
                    (shockObj as Creature).LoseAllGrasps();
                    self.Stun(20);
                }
            }
            if (shockObj.Submersion > 0f)
            {
                self.room.AddObject(new UnderwaterShock(self.room, self, self.mainBodyChunk.pos, 14, Mathf.Lerp(ModManager.MMF ? 0f : 200f, 1200f,0.25f), 0.2f + 1.9f, self, new Color(0.7f, 0.7f, 1f)));
            }
            data.Charge = chargepower / 2;
            if(data.Charge <= 0.05) { data.Charge = 0; }
            data.DischargeFac = -1; //won't shock again for the moment
            PLAYER_VALUES.Remove(self);
            PLAYER_VALUES.Add(self, data);
        }


        //control our magic mushrooms power
        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (self.SlugCatClass.ToString() != "Erroneous.Harbinger") { return; } //only harbinger can do this
            bool yes = PLAYER_VALUES.TryGetValue(self, out HarbingerData data);
            if (yes && self.input[0].y < 0 && self.canJump>0 && self.mushroomCounter>0)
            {
                data.DisableEffectsFac += Time.deltaTime;
                if (data.DisableEffectsFac > 1) { self.mushroomCounter = 0; data.DisableEffectsFac = 0; }
            }
            else
            {
                data.DisableEffectsFac = 0;
            }
            if (yes)
            {
                FrameSparkChance.TryGet(self, out float c);
                if (UnityEngine.Random.value < c*(Mathf.LerpUnclamped(0.3f,2f, data.Charge)) && data.Charge>0 && !self.dead)
                {
                    self.room.AddObject(new Spark(self.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                }
            }
            if (self.grabbedBy.Count>0 && yes && !self.dead)
            {
                if (data.DischargeFac != -1 && data.Charge > 0)
                {
                    self.room.AddObject(new Spark(self.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                    data.DischargeFac -= Time.deltaTime;
                    if (data.DischargeFac <= 0)
                    {
                        for (int k = 0; k < 40; k++)
                        {
                            self.room.AddObject(new Spark(self.grabbedBy[0].grabber.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                        }
                        Debug.Log("SHOCKERS!!!");
                        //shock the target
                        ShockTarget(self, self.grabbedBy[0].grabber);
                        self.room.AddObject(new ShockWave(self.bodyChunks[0].pos, 100f, 0.07f, 6, false));
                    }
                }
                
            }
            PLAYER_VALUES.Remove(self);
            PLAYER_VALUES.Add(self, data);
            self.exhausted = false; //we are immune to starving exhaustion
            if (self.dead) { self.mushroomCounter = 0; return; }
            if (self.wantToJump > 0 && self.input[0].pckp 
                && self.canJump <= 0 
                && self.mushroomCounter<=0 
                && self.bodyMode != Player.BodyModeIndex.Crawl 
                && self.bodyMode != Player.BodyModeIndex.CorridorClimb 
                && self.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut 
                && self.animation != Player.AnimationIndex.HangFromBeam 
                && self.animation != Player.AnimationIndex.ClimbOnBeam 
                && self.bodyMode != Player.BodyModeIndex.WallClimb 
                && self.bodyMode != Player.BodyModeIndex.Swimming  
                && self.animation != Player.AnimationIndex.AntlerClimb 
                && self.animation != Player.AnimationIndex.VineGrab
                && self.animation != Player.AnimationIndex.ZeroGPoleGrab
                && self.FoodInStomach>0)
            {
                MushroomTime.TryGet(self, out int time);
                self.mushroomCounter = time;
                self.wantToJump = 0;
                self.SubtractFood(1);
            }
        }

        //BOMB the player if they eat a mushroom
        private void Player_Eat(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
        {
            orig(self, edible);
            if(self.SlugCatClass.ToString() != "Erroneous.Harbinger") { return; } //only Harbinger is allergic to mushrooms... right?

            if(edible is Centipede) //give charge from eating BABY CENTIPEDES
            {
            if (PLAYER_VALUES.TryGetValue(self, out HarbingerData data))
            {
                    if ((edible as Centipede).TotalMass > data.Charge)
                    {
                        data.Charge = (edible as Centipede).TotalMass;
                        for (int k = 0; k < 20; k++)
                        {
                            self.room.AddObject(new Spark(self.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                        }
                        self.room.AddObject(new ZapCoil.ZapFlash(self.mainBodyChunk.pos, 2.5f));
                        self.room.PlaySound(SoundID.Centipede_Shock, self.mainBodyChunk.pos);
                    }
                    PLAYER_VALUES.Remove(self);
                    PLAYER_VALUES.Add(self, data);
                }
            }

            if (edible is JellyFish) //give charge from eating JELLYFISH
            {
                if (PLAYER_VALUES.TryGetValue(self, out HarbingerData data))
                {
                    if ((edible as JellyFish).TotalMass > data.Charge)
                    {
                        data.Charge = (edible as JellyFish).TotalMass;
                        for (int k = 0; k < 20; k++)
                        {
                            self.room.AddObject(new Spark(self.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                        }
                        self.room.AddObject(new ZapCoil.ZapFlash(self.mainBodyChunk.pos, 2.5f));
                        self.room.PlaySound(SoundID.Centipede_Shock, self.mainBodyChunk.pos);
                    }
                    PLAYER_VALUES.Remove(self);
                    PLAYER_VALUES.Add(self, data);
                }
            }

            if (edible is Mushroom
                && InedibleMushrooms.TryGet(self, out bool explode)
                && explode)
            {
                // Adapted from ScavengerBomb.Explode
                var room = self.room;
                var pos = self.mainBodyChunk.pos;
                var color = self.ShortCutColor();
                room.AddObject(new Explosion(room, self, pos, 7, 250f, 6.2f, 2f, 280f, 0.25f, self, 0.7f, 160f, 1f));
                room.AddObject(new Explosion.ExplosionLight(pos, 280f, 1f, 7, color));
                room.AddObject(new Explosion.ExplosionLight(pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                room.AddObject(new ExplosionSpikes(room, pos, 14, 30f, 9f, 7f, 170f, color));
                room.AddObject(new ShockWave(pos, 330f, 0.045f, 5, false));

                room.ScreenMovement(pos, default, 1.3f);
                room.PlaySound(SoundID.Bomb_Explode, pos);
                room.InGameNoise(new Noise.InGameNoise(pos, 9000f, self, 1f));
                self.Die();
            }
        }
        #endregion

        // Load any resources, such as sprites or sounds
        private void LoadResources(RainWorld rainWorld)
        {
            var atlas = Futile.atlasManager.LoadImage("atlases/harbingerhalo");
            Debug.Log("MY FUNKY IMAGE: "+(atlas==null).ToString());
        }
 
    }
}
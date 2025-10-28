using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;
using ClickableTransparentOverlay;
using ImGuiNET;

namespace Multi_ESP
{
    internal class Renderer : Overlay
    {
        private float yOffset = 20;
        public Vector2 screeenSize = new Vector2(1920, 1080);

        private ConcurrentQueue<Entity> entities = new ConcurrentQueue<Entity>();
        private Entity localPlayer = new Entity();
        private readonly object entityLock = new object();

        public int fov = 60;

        public bool glow = false;
        private bool enableFOV = false;
        public bool enableAntiFlash = false;
        public bool enableBHOP = false;
        public bool enableTriggerBot = false;
        private bool enableEsp = false;
        private bool enableBones = false;
        private bool enableName = false;
        private bool enableVisibilityCheck = false;
        private bool weaponEsp = false;
        private bool box = false;
        private bool drawLine = false;

        private float boneThickness = 4;

        private Vector4 enemyColor = new Vector4(1, 0, 0, 1);
        private Vector4 teamColor = new Vector4(0, 1, 0, 1);
        private Vector4 barColor = new Vector4(0, 1, 0, 1);
        private Vector4 nameColor = new Vector4(1, 1, 1, 1);
        private Vector4 hiddenColor = new Vector4(0, 0, 0, 1);
        private Vector4 BoneColor = new Vector4(1, 0, 2, 1);

        ImDrawListPtr drawList;

        private float windowRounding = 12.0f;
        private float frameRounding = 6.0f;
        private float grabRounding = 3.0f;
        private Vector4 accentColor = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
        private Vector4 backgroundColor = new Vector4(0.08f, 0.08f, 0.08f, 0.94f);

        protected override void Render()
        {
            ApplyDarkTheme();

            DrawOverlay(screeenSize);
            drawList = ImGui.GetWindowDrawList();

            if (enableEsp)
            {
                foreach (Entity entity in entities)
                {
                    if (EntityOnSceen(entity))
                    {
                        DrawHealthBar(entity);
                        if (box) DrawBox(entity);
                        if (drawLine) DrawLine(entity);
                        DrawNameAndWeapon(entity);
                        ScopedCheck(entity);
                        if (enableBones && entity.team != localPlayer.team) DrawBones(entity);
                    }
                }
            }

            ImGui.SetNextWindowSize(new Vector2(450, 600), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(new Vector2(20, 20), ImGuiCond.FirstUseEver);

            ImGui.Begin("CheaTIX v6 BETA", ImGuiWindowFlags.NoCollapse);

            ImGui.PushStyleColor(ImGuiCol.Text, accentColor);
            ImGui.TextColored(accentColor, "tgk: @breadlabs_dev");
            ImGui.PopStyleColor();

            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.BeginTabBar("Cheats", ImGuiTabBarFlags.None))
            {
                if (ImGui.BeginTabItem("Render"))
                {
                    ImGui.Spacing();

                    if (ImGui.CollapsingHeader("ESP Settings", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.Indent(10);
                        ImGui.Checkbox("Enable ESP", ref enableEsp);

                        if (enableEsp)
                        {
                            ImGui.Spacing();
                            //ImGui.Checkbox("Glow", ref glow);
                            ImGui.Checkbox("Box ESP", ref box);
                            ImGui.Checkbox("Lines", ref drawLine);
                            ImGui.Checkbox("Nicknames", ref enableName);
                            ImGui.Checkbox("Bone ESP", ref enableBones);
                            ImGui.Checkbox("Visibility Check", ref enableVisibilityCheck);
                            ImGui.Checkbox("Weapon ESP", ref weaponEsp);

                            ImGui.Spacing();
                            if (enableBones)
                            {
                                ImGui.SliderFloat("Bone Thickness", ref boneThickness, 1.0f, 10.0f, "%.1f");
                                if (ImGui.ColorEdit4("Bone Color", ref BoneColor, ImGuiColorEditFlags.NoInputs))
                                {
                                    BoneColor.W = 1.0f;
                                }
                            }
                        }
                        ImGui.Unindent(10);
                    }

                    ImGui.Spacing();

                    if (ImGui.CollapsingHeader("Colors"))
                    {
                        ImGui.Indent(10);
                        ImGui.ColorEdit4("Enemy Color", ref enemyColor, ImGuiColorEditFlags.NoInputs);
                        ImGui.ColorEdit4("Team Color", ref teamColor, ImGuiColorEditFlags.NoInputs);
                        ImGui.ColorEdit4("Health Bar", ref barColor, ImGuiColorEditFlags.NoInputs);
                        ImGui.ColorEdit4("Name Color", ref nameColor, ImGuiColorEditFlags.NoInputs);
                        ImGui.ColorEdit4("Hidden Color", ref hiddenColor, ImGuiColorEditFlags.NoInputs);
                        ImGui.Unindent(10);
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Aim"))
                {
                    ImGui.Spacing();
                    ImGui.Checkbox("Enable TriggerBot", ref enableTriggerBot);

                    if (enableTriggerBot)
                    {
                        ImGui.Indent(10);
                        ImGui.Text("TriggerBot is active");
                        ImGui.TextColored(new Vector4(0, 1, 0, 1), "Status: Ready");
                        ImGui.Unindent(10);
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Misc"))
                {
                    ImGui.Spacing();

                    //ImGui.Checkbox("Bunny Hop", ref enableBHOP);
                    ImGui.Checkbox("Anti Flash", ref enableAntiFlash);

                    ImGui.Spacing();
                    ImGui.Checkbox("FOV Changer", ref enableFOV);
                    if (enableFOV)
                    {
                        ImGui.Indent(10);
                        ImGui.SliderInt("FOV Value", ref fov, 58, 140, "%d°");
                        ImGui.Unindent(10);
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Info"))
                {
                    ImGui.Spacing();
                    ImGui.TextColored(accentColor, "System Information:");
                    ImGui.Text($"Entities: {entities.Count}");
                    ImGui.Text($"Local Player Health: {localPlayer.health}");
                    ImGui.Text($"FPS: {ImGui.GetIO().Framerate:0.##}");

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextDisabled("by mncrzz_dev");

            ImGui.End();
        }

        private void ApplyDarkTheme()
        {
            var style = ImGui.GetStyle();
            var colors = style.Colors;

            style.WindowRounding = windowRounding;
            style.ChildRounding = windowRounding;
            style.FrameRounding = frameRounding;
            style.GrabRounding = grabRounding;
            style.PopupRounding = windowRounding;
            style.ScrollbarRounding = 9.0f;
            style.TabRounding = frameRounding;

            colors[(int)ImGuiCol.Text] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
            colors[(int)ImGuiCol.WindowBg] = backgroundColor;
            colors[(int)ImGuiCol.ChildBg] = new Vector4(0.08f, 0.08f, 0.08f, 0.00f);
            colors[(int)ImGuiCol.PopupBg] = new Vector4(0.08f, 0.08f, 0.08f, 0.94f);
            colors[(int)ImGuiCol.Border] = new Vector4(0.43f, 0.43f, 0.50f, 0.50f);
            colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.16f, 0.16f, 0.16f, 0.54f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.20f, 0.20f, 0.20f, 0.54f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.26f, 0.26f, 0.26f, 0.54f);
            colors[(int)ImGuiCol.TitleBg] = new Vector4(0.04f, 0.04f, 0.04f, 1.00f);
            colors[(int)ImGuiCol.TitleBgActive] = accentColor;
            colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.00f, 0.00f, 0.00f, 0.51f);
            colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.14f, 0.14f, 0.14f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.02f, 0.02f, 0.02f, 0.53f);
            colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.31f, 0.31f, 0.31f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.41f, 0.41f, 0.41f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.51f, 0.51f, 0.51f, 1.00f);
            colors[(int)ImGuiCol.CheckMark] = accentColor;
            colors[(int)ImGuiCol.SliderGrab] = accentColor;
            colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.35f, 0.67f, 1.00f, 1.00f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.16f, 0.16f, 0.16f, 0.54f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.40f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.26f, 0.59f, 0.98f, 0.70f);
            colors[(int)ImGuiCol.Header] = new Vector4(0.26f, 0.59f, 0.98f, 0.31f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.80f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
            colors[(int)ImGuiCol.Separator] = new Vector4(0.43f, 0.43f, 0.50f, 0.50f);
            colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.10f, 0.40f, 0.75f, 0.78f);
            colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.10f, 0.40f, 0.75f, 1.00f);
            colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.26f, 0.59f, 0.98f, 0.20f);
            colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.67f);
            colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.26f, 0.59f, 0.98f, 0.95f);
            colors[(int)ImGuiCol.Tab] = new Vector4(0.08f, 0.08f, 0.08f, 0.86f);
            colors[(int)ImGuiCol.TabHovered] = new Vector4(0.26f, 0.59f, 0.98f, 0.80f);
            colors[(int)ImGuiCol.TabActive] = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);
            colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.08f, 0.08f, 0.08f, 0.97f);
            colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.14f, 0.14f, 0.14f, 1.00f);
            colors[(int)ImGuiCol.PlotLines] = new Vector4(0.61f, 0.61f, 0.61f, 1.00f);
            colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(1.00f, 0.43f, 0.35f, 1.00f);
            colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.26f, 0.59f, 0.98f, 0.35f);
            colors[(int)ImGuiCol.DragDropTarget] = new Vector4(1.00f, 1.00f, 0.00f, 0.90f);
            colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.26f, 0.59f, 0.98f, 1.00f);
            colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
            colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.80f, 0.80f, 0.80f, 0.20f);
            colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.80f, 0.80f, 0.80f, 0.35f);
        }

        bool EntityOnSceen(Entity entity)
        {
            if(entity.position2d.X > 0 && entity.position2d.X < screeenSize.X && entity.position2d.Y>0 && entity.position2d.Y < screeenSize.Y)
            {
                return true;
            }
            return false;
        }

        private void DrawBox(Entity entity)
        {
            float entityHeight = entity.position2d.Y - entity.viewPosition2D.Y;

            Vector2 rectTop = new Vector2(entity.viewPosition2D.X - entityHeight / 4, entity.viewPosition2D.Y);
            Vector2 rectBottom = new Vector2(entity.viewPosition2D.X + entityHeight / 4, entity.viewPosition2D.Y + entityHeight);

            Vector4 boxColor = localPlayer.team == entity.team ? teamColor : enemyColor;

            if (enableVisibilityCheck && localPlayer.team != entity.team)
            {
                boxColor = entity.spotted ? boxColor : hiddenColor;
            }

            drawList.AddRect(rectTop, rectBottom, ImGui.ColorConvertFloat4ToU32(boxColor));     
        }

        private void DrawLine(Entity entity)
        {
            Vector4 lineColor = localPlayer.team == entity.team ? teamColor : enemyColor;
            if (enableVisibilityCheck && localPlayer.team != entity.team)
            {
                lineColor = entity.spotted ? lineColor : hiddenColor;
            }

            drawList.AddLine(new Vector2(screeenSize.X / 2, screeenSize.Y), entity.position2d, ImGui.ColorConvertFloat4ToU32(lineColor));

        }

        private void DrawHealthBar(Entity entity)
        {
            float entityHeight = entity.position2d.Y - entity.viewPosition2D.Y;

            float boxLeft = entity.viewPosition2D.X - entityHeight / 4 + 0.01f;
            float boxRight = entity.viewPosition2D.X + entityHeight / 4 + 0.01f;

            float barPercentWidth = 0.05f;
            float barHeight = entityHeight * (entity.health / 100f);

            float barPixelWidth = barPercentWidth * (boxRight - boxLeft);

            Vector2 barTop = new Vector2(boxLeft - barPixelWidth, entity.position2d.Y - barHeight);
            Vector2 barBottom = new Vector2(boxLeft, entity.position2d.Y);
 
            drawList.AddRectFilled(barTop, barBottom, ImGui.ColorConvertFloat4ToU32(barColor));

        }
        private void DrawNameAndWeapon(Entity entity)
        {
            if (enableName)
            {
                float distance = entity.distance;

                float textScale = 0.8f / (distance * 0.1f);
                textScale = Math.Clamp(textScale, 0.5f, 2.0f) * 1.5f;
                Vector2 textLocation1 = new Vector2(entity.viewPosition2D.X, entity.position2d.Y - yOffset);
                ImGui.SetWindowFontScale(textScale);
                drawList.AddText(textLocation1, ImGui.ColorConvertFloat4ToU32(nameColor), $"{entity.name}");
            }
            if (weaponEsp)
            {
                Vector2 textLocation2 = new Vector2(entity.viewPosition2D.X, entity.position2d.Y);
                drawList.AddText(textLocation2, ImGui.ColorConvertFloat4ToU32(nameColor), $"GUN : {entity.currentWeaponName}");
            }
            ImGui.SetWindowFontScale(1.0f);
        
    }
        private void ScopedCheck(Entity entity)
        {
            Vector2 textLocation = new Vector2(entity.viewPosition2D.X, entity.position2d.Y + yOffset);
            if (entity.scoped)
            {
                drawList.AddText(textLocation, ImGui.ColorConvertFloat4ToU32(nameColor), $"SCOPPED");
            }
        }

        private void DrawBones(Entity entity)
        {
            uint uintColor = ImGui.ColorConvertFloat4ToU32(BoneColor);
            float currentBoneThickness;

            if (localPlayer.scoped)
            {
                currentBoneThickness = boneThickness;
            }
            else
            {
                currentBoneThickness = boneThickness / entity.distance;
            }

            drawList.AddLine(entity.bones2d[1], entity.bones2d[2], uintColor, currentBoneThickness);

            drawList.AddLine(entity.bones2d[1], entity.bones2d[3], uintColor, currentBoneThickness);

            drawList.AddLine(entity.bones2d[1], entity.bones2d[6], uintColor, currentBoneThickness);

            drawList.AddLine(entity.bones2d[3], entity.bones2d[4], uintColor, currentBoneThickness);

            drawList.AddLine(entity.bones2d[6], entity.bones2d[7], uintColor, currentBoneThickness);

            drawList.AddLine(entity.bones2d[4], entity.bones2d[5], uintColor, currentBoneThickness);

            drawList.AddLine(entity.bones2d[7], entity.bones2d[8], uintColor, currentBoneThickness);

            drawList.AddLine(entity.bones2d[1], entity.bones2d[0], uintColor, currentBoneThickness);

            drawList.AddLine(entity.bones2d[0], entity.bones2d[9], uintColor, currentBoneThickness);

            drawList.AddLine(entity.bones2d[0], entity.bones2d[11], uintColor, currentBoneThickness);

            drawList.AddLine(entity.bones2d[9], entity.bones2d[10], uintColor, currentBoneThickness);

            drawList.AddLine(entity.bones2d[11], entity.bones2d[12], uintColor, currentBoneThickness);

            drawList.AddCircle(entity.bones2d[2], (entity.position2d.Y - entity.viewPosition2D.Y) / 8.5f, uintColor);
            
        
        }

        public void UpdateEntities(IEnumerable<Entity> newEntities)
        {
            entities = new ConcurrentQueue<Entity>(newEntities);

        }

        public void UpdateLocalPlayer(Entity newEntity)
        {
            lock (entityLock)
            {
                localPlayer = newEntity;
            }
        }

        public Entity GetLocalPlayer()
        {
            lock (entityLock){
                return localPlayer;
            }
        }
        void DrawOverlay(Vector2 screenSize)
        {
            ImGui.SetNextWindowSize(screenSize);
            ImGui.SetNextWindowPos(new Vector2(0,0));
            ImGui.Begin("CheaTIX", ImGuiWindowFlags.NoDecoration
                | ImGuiWindowFlags.NoBackground
                | ImGuiWindowFlags.NoBringToFrontOnFocus
                | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoInputs
                | ImGuiWindowFlags.NoCollapse
                | ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse
                );

        }

    }
}
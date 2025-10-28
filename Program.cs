using Multi_ESP;
using Swed64;
using System.Numerics;
using System.Runtime.InteropServices;
using static CS2Dumper.Schemas.ClientDll;

Swed swed = new Swed("cs2");

IntPtr client = swed.GetModuleBase("client.dll");

IntPtr forceJumpAddres = client + 0x1BE5FE0;

Renderer renderer = new Renderer();
Thread renderThread = new Thread(new ThreadStart(renderer.Start().Wait));

renderThread.Start();

Vector2 screenSize = renderer.screeenSize;

List<Entity> entities = new List<Entity>();
Entity localPlayer = new Entity();

int dwEntityList = (int)CS2Dumper.Offsets.ClientDll.dwEntityList; 
int dwLocalPlayerPawn = (int)CS2Dumper.Offsets.ClientDll.dwLocalPlayerPawn;
int dwViewMatrix = (int)CS2Dumper.Offsets.ClientDll.dwViewMatrix;
int m_pCameraServices = (int)C_BasePlayerPawn.m_pCameraServices;
int m_iFOV = (int)CCSPlayerBase_CameraServices.m_iFOV;
int m_bIsScoped = (int)C_CSPlayerPawn.m_bIsScoped;
int m_vOldOrigin = (int)C_BasePlayerPawn.m_vOldOrigin;
int m_iTeamNum = (int)C_BaseEntity.m_iTeamNum;
int m_lifeState = (int)C_BaseEntity.m_lifeState;
int m_hPlayerPawn = (int)CCSPlayerController.m_hPlayerPawn;
int m_vecViewOffset = (int)C_BaseModelEntity.m_vecViewOffset;
int m_iHealth = (int)C_BaseEntity.m_iHealth;
int m_iszPlayerName = (int)CBasePlayerController.m_iszPlayerName;
int m_entitySpottedState = (int)C_C4.m_entitySpottedState;
int m_bSpotted = (int)EntitySpottedState_t.m_bSpotted;
int m_bOldIsScoped = (int)C_CSPlayerPawn.m_bOldIsScoped;
int m_modelState = (int)CSkeletonInstance.m_modelState;
int m_pGameSceneNode = (int)C_BaseEntity.m_pGameSceneNode;
int m_pClippingWeapon = (int)C_CSPlayerPawn.m_pClippingWeapon;
int m_iItemDefinitionIndex = (int)C_EconItemView.m_iItemDefinitionIndex;
int m_AttributeManager = (int)C_Chicken.m_AttributeManager;
int m_Item = (int)C_AttributeContainer.m_Item;
int dwForceAttack = (int)CS2Dumper.Buttons.attack;
int m_iIDEntIndex = (int)C_CSPlayerPawn.m_iIDEntIndex;
int m_flFlashBangTime = (int)C_CSPlayerPawnBase.m_flFlashBangTime;
const int SPACE_BAR = 0x20;
const uint STANDING = 65665;
const uint CROUCHING = 65667;
const uint PLUS_JUMP = 65537;
const uint MINUS_JUMP = 256;


while (true)
{
    entities.Clear();

    IntPtr entityList = swed.ReadPointer(client, dwEntityList);
    IntPtr listEntry = swed.ReadPointer(entityList, 0x10);
    IntPtr localPlayerPawn = swed.ReadPointer(client, dwLocalPlayerPawn);
    IntPtr cameraServices = swed.ReadPointer(localPlayerPawn, m_pCameraServices);

    localPlayer.team =  swed.ReadInt(localPlayerPawn , m_iTeamNum);

    uint currentFov = swed.ReadUInt(cameraServices + m_iFOV);
    bool isScoped = swed.ReadBool(localPlayerPawn, m_bIsScoped);
    uint desiredFov = (uint)renderer.fov;

    int team = swed.ReadInt(localPlayerPawn, m_iTeamNum);
    int entIndex = swed.ReadInt(localPlayerPawn, m_iIDEntIndex);

    uint fFlag = swed.ReadUInt(dwLocalPlayerPawn, 0x3C8);

    float flashDuracation = swed.ReadFloat(dwLocalPlayerPawn, m_flFlashBangTime);

    if (renderer.enableAntiFlash)
    {
        swed.WriteFloat(localPlayerPawn, m_flFlashBangTime, 0);
    }

    if(GetAsyncKeyState(SPACE_BAR) < 0 && renderer.enableBHOP)
    {
        if(fFlag == STANDING || fFlag == CROUCHING)
        {
            Thread.Sleep(1);
            swed.WriteUInt(forceJumpAddres, PLUS_JUMP);
        }
        else
        {
            swed.WriteUInt(forceJumpAddres, MINUS_JUMP);
        }
    }

    if(entIndex != -1)
    {
        IntPtr listEntry3 = swed.ReadPointer(entityList, 0x8 * ((entIndex & 0x7FFF) >> 9) + 0x10);
        IntPtr currentPawn2 = swed.ReadPointer(listEntry3, 0x78 * (entIndex & 0x1FF));
        int entityTeam = swed.ReadInt(currentPawn2 + m_iTeamNum);

        if(team != entityTeam)
        {
            if(renderer.enableTriggerBot)
            {
                swed.WriteInt(client, dwForceAttack, 65537);
                Thread.Sleep(10);
                swed.WriteInt(client, dwForceAttack, 256);
                Thread.Sleep(10);
            }
        }
    }

    if(!isScoped && currentFov != desiredFov)
    {
        swed.WriteUInt(cameraServices + m_iFOV, desiredFov);
    }

    for( int i = 1; i < 64; i++)
    {
        if (listEntry == IntPtr.Zero) continue;

        IntPtr currentController = swed.ReadPointer(listEntry, i * 0x78);

        if (currentController == IntPtr.Zero) continue;

        int pawnHandle = swed.ReadInt(currentController, m_hPlayerPawn);

        if (pawnHandle == 0) continue;

        IntPtr listEntry2 = swed.ReadPointer(entityList, 0x8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10);

        IntPtr currentPawn = swed.ReadPointer(listEntry2, 0x78 * (pawnHandle & 0x1FF));

        int lifeState = swed.ReadInt(currentPawn, m_lifeState);
        if (lifeState !=256) continue;

        IntPtr sceneNode = swed.ReadPointer(currentPawn, m_pGameSceneNode);
        IntPtr boneMatrix = swed.ReadPointer(sceneNode, m_modelState + 0x80);

        float[] viewMatrix = swed.ReadMatrix(client + dwViewMatrix);

        IntPtr currentWeapon = swed.ReadPointer(currentPawn, m_pClippingWeapon);

        short weponDefenitionIndex = swed.ReadShort(currentWeapon, m_AttributeManager + m_Item + m_iItemDefinitionIndex);

        Entity entity = new Entity();

        entity.spotted = swed.ReadBool(currentPawn, m_entitySpottedState + m_bSpotted);
        entity.scoped = swed.ReadBool(currentPawn, m_bOldIsScoped);

        entity.name = swed.ReadString(currentController , m_iszPlayerName, 16).Split("\0")[0];

        entity.team = swed.ReadInt(currentPawn, m_iTeamNum);
        entity.health = swed.ReadInt(currentPawn, m_iHealth);

        entity.position = swed.ReadVec(currentPawn, m_vOldOrigin);
        entity.viewOffset = swed.ReadVec(currentPawn, m_vecViewOffset);

        entity.position2d = Calculate.WordToScreen(viewMatrix, entity.position, screenSize);
        entity.viewPosition2D = Calculate.WordToScreen(viewMatrix, Vector3.Add(entity.position, entity.viewOffset), screenSize);

        entity.distance = Vector3.Distance(entity.position, localPlayer.position);

        entity.bones = Calculate.ReadBones(boneMatrix, swed);

        entity.bones2d = Calculate.ReadBones2d(entity.bones, viewMatrix, renderer.screeenSize);
        entity.currentWeaponIndex = weponDefenitionIndex;
        entity.currentWeaponName = Enum.GetName(typeof(Weapon), weponDefenitionIndex);
        entities.Add(entity);
        Console.WriteLine($"entity pos: {entity.position.X} ,{entity.position.Y}, {entity.position.Z}. team : {entity.team}. ID: {i} , weapon: {entity.currentWeaponName}");
    }

    renderer.UpdateLocalPlayer(localPlayer);
    renderer.UpdateEntities(entities);
}

[DllImport("user32.dll")]
static extern short GetAsyncKeyState(int vKey);
using System;
using System.Collections;
using System.Reflection;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Newtonsoft.Json;
using KingmakerMountedCombat.Diagnostics.WorkingLoadPrototype;

namespace KingmakerMountedCombat.Diagnostics
{
    // Original metadata bindings, not proprietary implementation. Construction
    // reflects only already loaded types; it never invokes a native method.
    internal sealed class NativeWorkingLoadContract
    {
        internal static readonly Guid GameMvid = new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7");
        internal static readonly Guid JsonMvid = new Guid("4f213745-d60e-425f-b763-df319c826ddf");
        internal readonly MethodInfo[] Owners;
        internal readonly MethodBase LoaderConstructor;
        internal readonly MethodInfo Start, CreateSerializer, DeserializePlayer, DeserializeParty, LogException, ScenePostLoad;
        internal readonly FieldInfo IteratorManager, IteratorSave, IteratorSmoke, IteratorLoader, IteratorTask;
        internal readonly FieldInfo ClosureLoader, PartyClosure, PartyResult, PlayerJson, PartyJson;
        internal readonly FieldInfo GameState, PlayerState, CrossScene, MainReference, MainId, Entities, EntityId, HoldingScene;
        internal readonly FieldInfo Converters, ContractResolver, ReferenceResolver, CollectionsEnabled;
        internal readonly Type LoaderType, PlayerType, SceneType, SerializerType;
        private static FieldInfo Field(Module module, int token)
        {
            var field = module.ResolveField(token);
            Require(field != null && field.MetadataToken == token && field.Module == module, "Unpinned native field.");
            return field;
        }
        internal NativeWorkingLoadContract()
        {
            var game = typeof(Game).Module; var json = typeof(JsonSerializer).Module;
            Require(game.ModuleVersionId == GameMvid && json.ModuleVersionId == JsonMvid, "Installed loader/serializer module changed.");
            Owners = new[] { Method(game, 0x0600BF00), Method(game, 0x0600BF1A), Method(game, 0x0600BF1D), Method(game, 0x06008054), Method(game, 0x06000DC7) };
            Require(Owners[0].ReturnType == typeof(bool), "Native iterator signature changed.");
            for (int i = 0; i < Owners.Length; i++) Require(!Owners[i].IsStatic && Owners[i].GetParameters().Length == 0 &&
                (i == 0 || Owners[i].ReturnType == typeof(void)), "Native callback signature changed.");
            IteratorManager = Field(game, 0x04008CF5); IteratorSave = Field(game, 0x04008CF4); IteratorSmoke = Field(game, 0x04008CF6);
            IteratorLoader = Field(game, 0x04008CFC); IteratorTask = Field(game, 0x04008CFD);
            ClosureLoader = Field(game, 0x04008D0F); PartyClosure = Field(game, 0x04008D14); PartyResult = Field(game, 0x04008D13);
            PlayerJson = Field(game, 0x04008D10); PartyJson = Field(game, 0x04008D11);
            GameState = Field(game, 0x040006B6); PlayerState = Field(game, 0x0400533C); CrossScene = Field(game, 0x04000726);
            MainReference = Field(game, 0x0400073B); MainId = Field(game, 0x040054DB); Entities = Field(game, 0x04005322);
            EntityId = Field(game, 0x04005329); HoldingScene = Field(game, 0x0400532E); CollectionsEnabled = Field(game, 0x0400549D);
            Converters = Field(json, 0x040000EA); ContractResolver = Field(json, 0x040000EB); ReferenceResolver = Field(json, 0x040000F0);
            LoaderConstructor = game.ResolveMethod(0x06008051); Start = Method(game, 0x06008053); ScenePostLoad = Method(game, 0x06007E7F);
            CreateSerializer = game.ResolveMethod(0x0A002E66) as MethodInfo;
            DeserializePlayer = game.ResolveMethod(0x2B0012DA) as MethodInfo;
            DeserializeParty = game.ResolveMethod(0x2B0012DB) as MethodInfo;
            LogException = game.ResolveMethod(0x0A0005D6) as MethodInfo;
            LoaderType = IteratorLoader.FieldType; PlayerType = PlayerState.FieldType; SceneType = CrossScene.FieldType; SerializerType = typeof(JsonSerializer);
            Require(IteratorManager.DeclaringType == Owners[0].DeclaringType && IteratorManager.FieldType == typeof(SaveManager) &&
                IteratorSave.FieldType == typeof(SaveInfo) && IteratorSmoke.FieldType == typeof(bool) && IteratorTask.FieldType == typeof(System.Threading.Tasks.Task) &&
                ClosureLoader.DeclaringType == Owners[1].DeclaringType && ClosureLoader.FieldType == LoaderType &&
                PartyClosure.DeclaringType == Owners[2].DeclaringType && PartyClosure.FieldType == Owners[1].DeclaringType &&
                PartyResult.FieldType == SceneType && Owners[3].DeclaringType == LoaderType && Owners[4].DeclaringType == PlayerType &&
                MainId.FieldType == typeof(string) && EntityId.FieldType == typeof(string) && HoldingScene.FieldType == SceneType &&
                CollectionsEnabled.IsStatic && CollectionsEnabled.FieldType == typeof(bool) &&
                (CollectionsEnabled.DeclaringType.Attributes & TypeAttributes.BeforeFieldInit) == 0 &&
                CollectionsEnabled.DeclaringType.TypeInitializer?.MetadataToken == 0x060082A1, "Native field ownership/type changed.");
            Require(CreateSerializer != null && CreateSerializer.IsStatic && CreateSerializer.ReturnType == SerializerType &&
                CreateSerializer.GetParameters().Length == 1 && CreateSerializer.GetParameters()[0].ParameterType == typeof(JsonSerializerSettings) &&
                DeserializePlayer != null && DeserializePlayer.ReturnType == PlayerType && DeserializeParty != null && DeserializeParty.ReturnType == SceneType &&
                Start.ReturnType == typeof(System.Threading.Tasks.Task) && ScenePostLoad.ReturnType == typeof(void) &&
                LogException != null && LogException.IsStatic && LogException.ReturnType == typeof(void), "Native producer signatures changed.");
        }
        private static MethodInfo Method(Module module, int token)
        {
            var method = module.ResolveMethod(token) as MethodInfo;
            Require(method != null && method.MetadataToken == token && method.Module == module, "Unpinned native owner."); return method;
        }
        internal object CurrentPlayer(object game) { var state = GameState.GetValue(game); return state == null ? null : PlayerState.GetValue(state); }
        internal static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }

    internal sealed class NativeWorkingLoadFields : IFieldObservationSource
    {
        private readonly NativeWorkingLoadContract contract;
        internal NativeWorkingLoadFields(NativeWorkingLoadContract contract) { this.contract = contract ?? throw new ArgumentNullException(nameof(contract)); }
        public string MainId(object player) { var reference = contract.MainReference.GetValue(player); return reference == null ? null : (string)contract.MainId.GetValue(reference); }
        public object CurrentScene(object player) => contract.CrossScene.GetValue(player);
        public int EntityCount(object scene) => ((IList)contract.Entities.GetValue(scene)).Count;
        public object EntityAt(object scene, int index) => ((IList)contract.Entities.GetValue(scene))[index];
        public string EntityId(object entity) => (string)contract.EntityId.GetValue(entity);
        public object HoldingScene(object entity) => contract.HoldingScene.GetValue(entity);
        public string TypeLabel(object value) => value.GetType().FullName;
        public int ConverterCount(object serializer) => (contract.Converters.GetValue(serializer) as IList)?.Count ?? 0;
        public object ConverterAt(object serializer, int index) => ((IList)contract.Converters.GetValue(serializer))[index];
        public bool IsExactCollectionsConverter(object value) => value.GetType() == contract.CollectionsEnabled.DeclaringType;
        public bool InitializedCollectionsEnablement(object exactConverter)
        {
            NativeWorkingLoadContract.Require(IsExactCollectionsConverter(exactConverter), "Native converter enablement is unavailable.");
            return (bool)contract.CollectionsEnabled.GetValue(null);
        }
        public string ContractResolverType(object serializer) => contract.ContractResolver.GetValue(serializer)?.GetType().FullName;
        public string ExistingReferenceResolverType(object serializer) => contract.ReferenceResolver.GetValue(serializer)?.GetType().FullName;
    }
}

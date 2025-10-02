using Sims3.Gameplay;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CustomContent;
using Sims3.UI;
using Sims3.UI.GameEntry;

namespace Eca
{
    internal static class RotateLot
    {
        [Tunable]
        private static readonly bool instantiator;

        private static Lot ActiveBuildBuyLot => LotManager.sActiveBuildBuyLot;

        private static LotRotationAngle LotRotationAngle { get; set; } = LotRotationAngle.kLotRotateNone;

        static RotateLot() => InWorldState.InWorldSubStateChanging += OnInWorldSubStateChanging;

        private static void OnInWorldSubStateChanging(InWorldState.SubState previousState, InWorldState.SubState newState)
        {
            int commandAddRemoveState = 0;
            switch (previousState)
            {
                case InWorldState.SubState.BuildMode:
                case InWorldState.SubState.BuyMode:
                    commandAddRemoveState = -1;
                    break;
            }
            switch (newState)
            {
                case InWorldState.SubState.BuildMode:
                case InWorldState.SubState.BuyMode:
                    commandAddRemoveState = commandAddRemoveState == -1 ? 0 : 1;
                    break;
            }
            switch (commandAddRemoveState)
            {
                case -1:
                    UnregisterCommands();
                    break;
                case 1:
                    bool _ = RegisterCommands() || UnregisterCommands();
                    break;
            }
        }

        private static bool RegisterCommands() =>
            CommandSystem.RegisterCommand("rotateLot", "Rotates the active build/buy lot. Usage: rotateLot (non-square lots only), rotateLot+ (positive rotation), or rotateLot- (negative rotation)", (parameters) =>
            {
                bool failure = false;
                Lot activeBuildBuyLot = ActiveBuildBuyLot;
                if (!failure && (failure = activeBuildBuyLot == null))
                    SimpleMessageDialog.Show("rotateLot", "The active build/buy lot is not found.");
                if (!failure && (failure = parameters?.Length > 0))
                    SimpleMessageDialog.Show("rotateLot", "The given parameters are unnecessary and not valid.\nUse 'rotateLot', 'rotateLot+', or 'rotateLot-'.");
                if (failure)
                    return -1;
                UIBinInfo binInfo = BinCommon.CreateInWorldBinInfo(activeBuildBuyLot.LotId);
                LotRotationAngle lotRotationAngle = LotRotationAngle;
                if ((int)binInfo.LotSizeX != (int)binInfo.LotSizeY)
                    lotRotationAngle = LotRotationAngle.kLotRotate180;
                if (failure = lotRotationAngle == LotRotationAngle.kLotRotateNone || lotRotationAngle == LotRotationAngle.kLotRotateAuto)
                    SimpleMessageDialog.Show("rotateLot", "The determined lot rotation angle is invalid for the selected build/buy lot.\nUse 'rotateLot+' or 'rotateLot-'.");
                if (failure)
                    return -1;
                ObjectGuid oneShotFunctionTask =
                Simulator.AddObject(new Sims3.Gameplay.OneShotFunctionTask(delegate
                {
                    failure = false;
                    try
                    {
                        if (failure = activeBuildBuyLot != ActiveBuildBuyLot)
                            SimpleMessageDialog.Show("rotateLot", "The current build/buy lot differs from the selected build/buy lot.");
                        if (failure)
                            return;
                        ScreenCaptureOverlay.Display(show: true, synchronous: true);
                        ProgressDialog.Show(Localization.LocalizeString("UI/Caption/Global:Processing"), delay: false);
                        LotPosition lotPosition = LotPosition.kLotPositionCenter;
                        Responder.Instance.EditTownModel.BeginMoveRotateLotContents(binInfo, activeBuildBuyLot.LotId, out _);
                        Responder.Instance.EditTownModel.RotateLotContents(ref lotRotationAngle, ref lotPosition);
                        Responder.Instance.EditTownModel.EndMoveRotateLotContents();
                        World.SetActiveLot(0L);
                        World.SetActiveLot(activeBuildBuyLot.LotId);
                        activeBuildBuyLot.SetDisplayLevel(activeBuildBuyLot.RoofLevel);
                    }
                    finally
                    {
                        if (!failure)
                        {
                            ProgressDialog.Close();
                            ScreenCaptureOverlay.Display(show: false, synchronous: true);
                        }
                    }
                }));
                LotRotationAngle = LotRotationAngle.kLotRotateNone;
                return oneShotFunctionTask.IsValid ? 1 : -1;
            }) &&
            CommandSystem.RegisterCommand("rotateLot+", "Rotates the active build/buy lot in positive direction.", (parameters) =>
            {
                LotRotationAngle = LotRotationAngle.kLotRotate90;
                return CommandSystem.ExecuteCommandString("rotateLot") ? 1 : -1;
            }, true) &&
            CommandSystem.RegisterCommand("rotateLot-", "Rotates the active build/buy lot in negative direction.", (parameters) =>
            {
                LotRotationAngle = LotRotationAngle.kLotRotate270;
                return CommandSystem.ExecuteCommandString("rotateLot") ? 1 : -1;
            }, true);

        private static bool UnregisterCommands()
        {
            CommandSystem.UnregisterCommand("rotateLot-");
            CommandSystem.UnregisterCommand("rotateLot+");
            CommandSystem.UnregisterCommand("rotateLot");
            return true;
        }
    }
}
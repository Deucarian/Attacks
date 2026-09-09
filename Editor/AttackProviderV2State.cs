using System;
using System.Collections.Generic;
using System.Globalization;
using Deucarian.Attacks.Authoring;
using Deucarian.Editor;
using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor
{
    internal sealed class AttackProviderV2State : GameContentAuthoringProviderSessionState<AttackAuthoringState>
    {
        public int PreviewSourceContextIndex;
        public int PreviewTargetContextIndex;
        public int LastPreviewAudioPhase = -1;

        public void BeginCreate()
        {
            Creating = true;
            DetailScroll = Vector2.zero;
            WizardStep = 0;
            ClearEditingState();
            LastPreviewAudioPhase = -1;
            PreviewStatus = "Previewing draft";
        }

        public void LeaveCreate()
        {
            Creating = false;
            DetailScroll = Vector2.zero;
            LastPreviewAudioPhase = -1;
            PreviewStatus = "Previewing selected attack";
        }

        protected override void OnPreviewStopped()
        {
            LastPreviewAudioPhase = -1;
        }

        protected override void OnProviderSessionReset()
        {
            LastPreviewAudioPhase = -1;
        }

        protected override void OnPreviewSourceChanged()
        {
            PreviewSourceContextIndex = 0;
            PreviewTargetContextIndex = 0;
            LastPreviewAudioPhase = -1;
        }
    }
}

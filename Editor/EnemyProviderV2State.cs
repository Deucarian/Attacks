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
    internal sealed class EnemyProviderV2State : GameContentAuthoringProviderSessionState<EnemyAuthoringState>
    {
        public void BeginCreate()
        {
            Creating = true;
            DetailScroll = Vector2.zero;
            WizardStep = 0;
            ClearEditingState();
            PreviewStatus = "Previewing draft";
        }

        public void LeaveCreate()
        {
            Creating = false;
            DetailScroll = Vector2.zero;
            PreviewStatus = "Previewing selected enemy";
        }
    }
}

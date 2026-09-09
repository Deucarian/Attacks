using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Deucarian.Attacks.Authoring;
using Deucarian.Editor;
using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Attacks.Editor
{
    internal sealed class WaveProviderV2State : GameContentAuthoringProviderSessionState<WaveAuthoringState>
    {
        public void BeginCreate()
        {
            Creating = true;
            WizardStep = 0;
            DetailPage = 0;
            DetailScroll = Vector2.zero;
            ClearEditingState();
            PreviewStatus = "Previewing draft wave";
        }
    }
}

// Copyright (c) 2018 - Loom Network. All rights reserved.
// https://loomx.io/



using System;
using System.Collections;
using System.Collections.Generic;
using LoomNetwork.CZB.Common;
using UnityEngine;

namespace LoomNetwork.CZB.Gameplay
{
    public interface ICameraManager
    {
        bool IsFading { get; }
        Enumerators.FadeState CurrentFadeState { get; }

		void FadeIn(Action callback = null, int level = 0);
		void FadeIn(float fadeTo, int level = 0);
        void FadeOut(Action callback = null, int level = 0, bool immediately = false);
    }
}
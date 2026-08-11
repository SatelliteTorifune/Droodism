using System;
using ModApi.GameLoop;
using UnityEngine;

namespace Assets.Scripts.Droodism.BackGround
{
    public class BackGroundCalulator:MonoBehaviourBase
    {
        public static BackGroundCalulator Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!Game.InFlightScene)
            {
                return;
            }
        }
    }   
}
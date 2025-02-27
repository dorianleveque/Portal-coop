﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WasaaMP {
    public class Fizzler : MonoBehaviour {

        public float speedX = 0.1f;
        public float speedY = 0.1f;
        public float curX, curY;

        // Start is called before the first frame update
        void Start () {
            curX = GetComponent<Renderer> ().material.mainTextureOffset.x;
            curY = GetComponent<Renderer> ().material.mainTextureOffset.y;
        }

        // Update is called once per frame
        void Update () {
            curX += Time.deltaTime * speedX;
            curY += Time.deltaTime * speedY;
            GetComponent<Renderer> ().material.SetTextureOffset ("_MainTex", new Vector2 (curX, curY));
        }

        void OnTriggerEnter (Collider other) {
            if (other.gameObject.GetComponent<TestCubeChamber> ()) {
                other.gameObject.GetComponent<Animator> ().SetTrigger ("play");
                Destroy (other.gameObject, 1f);
            }
        }
    }
}
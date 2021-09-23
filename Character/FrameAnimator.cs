using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FrameAnimation
{
    public bool loop = false;
    [Range(0, 60)] public int FPS = 12;
    public List<Sprite> frames = new List<Sprite>();
}

public class FrameAnimator : MonoBehaviour
{
    //Private variables
    private SpriteRenderer spriteRenderer;
    private FrameAnimation c_FrameAnimation;
    private bool c_Loop;
    private List<Sprite> c_Frames = new List<Sprite>();
    private float c_SPF;
    private int c_FrameIndex;
    private bool c_InsertPlay;

    //Frame index for other script to see its progress
    public int GetCurrentFrameIndex() { return c_FrameIndex; }

    //Initialization
    private void Start() 
    {
        //Get current sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    //Play animation using the data
    public void Play(FrameAnimation frameAnimation, bool exclusive)
    {
        //If there is an exclusive play ongoing, skip
        if (c_InsertPlay && !exclusive) { return; }

        //Set exclusive play
        c_InsertPlay = exclusive;

        //If there is a same animation playing, Skip
        if (c_Frames == frameAnimation.frames) { return; }

        //Load animation in local variable
        c_FrameAnimation = frameAnimation;

        //Load data in local variable
        LoadAnimationData();

        //Skip if there is no animation
        if (c_Frames.Count <= 0) { return; }

        //Stop any ongoing animation
        CancelInvoke();

        //Play animation
        InvokeRepeating(nameof(PlayFrames), 0, c_SPF);
    }

    //Play animation using the data
    public void Play(FrameAnimation frameAnimation)
    {
        //If there is an exclusive play ongoing, skip
        if (c_InsertPlay) { return; }

        //If there is a same animation playing, Skip
        if (c_Frames == frameAnimation.frames) { return; }

        //Load animation in local variable
        c_FrameAnimation = frameAnimation;

        //Load data in local variable
        LoadAnimationData();

        //Skip if there is no animation
        if (c_Frames.Count <= 0) { return; }

        //Stop any ongoing animation
        CancelInvoke();

        //Play animation
        InvokeRepeating(nameof(PlayFrames), 0, c_SPF);
    }

    //Stop animation
    public void Stop() { c_Frames = null; c_InsertPlay = false; CancelInvoke(); }

    //Load animation data from current frame animation into local variables
    public void LoadAnimationData() 
    {
        //Load frames of the animation
        c_Frames = c_FrameAnimation.frames;

        //Calculate seconds per frame
        c_SPF = 1 / (float)c_FrameAnimation.FPS;

        //Load loop status
        c_Loop = c_FrameAnimation.loop;

        //Reset frame index
        c_FrameIndex = 0;
    }

    //Play one frame
    private void PlayFrames()
    {
        //Change the frame of the sprite renderer
        spriteRenderer.sprite = c_Frames[c_FrameIndex];

        //Change current frame index to the next one
        c_FrameIndex++;

        //If the next frame index is out of bounds
        if (c_FrameIndex >= c_Frames.Count || c_FrameIndex < 0)
        {
            //If the animation is looping, set the index to zero
            if (c_Loop) { c_FrameIndex = 0; }

            //Or Stop the animation
            else { c_Frames = null; c_InsertPlay = false; CancelInvoke(); }
        }
    }
}

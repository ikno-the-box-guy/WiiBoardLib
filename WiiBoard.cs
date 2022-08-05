namespace WiiBoardLib;

using SharpDX.DirectInput;
using System;
using System.Diagnostics;
using System.Numerics;

/// <summary>
/// Class <see cref="WiiBoard"/>, requires <see cref="SharpDX.DirectInput"/>.
/// </summary>
public class WiiBoard
{
    private static DirectInput input = new DirectInput();
    private Vector2 normalizer;
    
    /// <summary>
    ///  The <see cref="Joystick"/> object assosiated with the <see cref="WiiBoard"/>.
    /// </summary>
    public Joystick Gamepad { get; private set; }

    /// <summary>
    /// The area in which the balancepoint is in the center. <br/>
    /// It is good practice to not count inputs that are inside of the <see cref="Deadzone"/> to accomodate for slight innacuracies in the state readings.
    /// </summary>
    public float Deadzone { get; set; }

    /// <summary>
    /// Constructor for the <see cref="WiiBoard"/> object.
    /// Requires the <paramref name="deviceGuid"/> which can be retreived with the help of <see cref="SharpDX.DirectInput"/>.
    /// </summary>
    public WiiBoard(Guid deviceGuid, float deadzone, bool calibrate = true)
    {
        Gamepad = new Joystick(input, deviceGuid);
        Gamepad.Acquire();

        Deadzone = deadzone;
        
        if (calibrate)
            Calibrate();
    }

    /// <summary>
    /// Automatically searches for a connected wiiboard and creates a <see cref="WiiBoard"/> object from it.<br/> 
    /// If no <see cref="WiiBoard"/> can be found within <paramref name="timeout"/> seconds an exception will be thrown.
    /// </summary>
    /// <returns>
    /// the <see cref="WiiBoard"/> object if a connected <see cref="WiiBoard"/> has been found.
    /// </returns>
    /// <exception cref="TimeoutException">
    /// Thrown when no connected wiiboard can be found.
    /// </exception>
    public static WiiBoard FindWiiBoard(double timeout, float deadzone, bool calibrate = true)
    {
        DeviceInstance? wiiboard = null;
        Stopwatch watch = new();
        watch.Start();

        while(wiiboard == null)
        {
            if (watch.Elapsed.TotalSeconds > timeout)
                throw new TimeoutException("Could not locate the wiiboard.");

            foreach (var device in input.GetDevices())
            {
                if (device.ProductName == "Nintendo RVL-CNT-01")
                {
                    wiiboard = device;
                    break;
                }
            }
        }

        return new WiiBoard(wiiboard.InstanceGuid, deadzone, calibrate);
    }

    /// <summary>
    /// Calibrates the <see cref="WiiBoard"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="WiiBoard"/> may not be touched when being calibrated.
    /// </remarks>
    public void Calibrate()
    {
        var state = Gamepad.GetCurrentState();
        normalizer = new Vector2(state.X, state.Y);
    }

    /// <summary>
    /// Gets the current state of the <see cref="WiiBoard"/>, this state is the balancepoint.
    /// </summary>
    /// <returns>
    /// a <see cref="Vector2"/> containg the state of the <see cref="WiiBoard"/>.
    /// </returns>
    public Vector2 GetState() 
    {
        var state = Gamepad.GetCurrentState();
        return new Vector2(state.X / normalizer.X - 1, -(state.Y / normalizer.Y - 1)); // Flip Y around because uhh some dumb stuff just turst me on this one okay
    }

    /// <summary>
    /// Whether or not the balancepoint is in the right area of the <see cref="WiiBoard"/> and outside of the <see cref="Deadzone"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the balancepoint is at the right of the <see cref="WiiBoard"/> and outside of the <see cref="Deadzone"/>; otherwise, false.
    /// </returns>
    public bool GetRight()
    {
        return GetState().X >= 0 && !InDeadZone();
    }

    /// <summary>
    /// Whether or not the balancepoint is in the left area of the <see cref="WiiBoard"/> and outside of the <see cref="Deadzone"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the balancepoint is at the left of the <see cref="WiiBoard"/> and outside of the <see cref="Deadzone"/>; otherwise, false.
    /// </returns>
    public bool GetLeft()
    {
        return GetState().X < 0 && !InDeadZone();
    }

    /// <summary>
    /// Whether or not the balancepoint is in the top area of the <see cref="WiiBoard"/> and outside of the <see cref="Deadzone"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the balancepoint is at the top of the <see cref="WiiBoard"/> and outside of the <see cref="Deadzone"/>; otherwise, false.
    /// </returns>
    public bool GetTop()
    {
        return GetState().Y >= 0 && !InDeadZone();
    }

    /// <summary>
    /// Whether or not the balancepoint is in the bottom area of the <see cref="WiiBoard"/> and outside of the <see cref="Deadzone"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the balancepoint is at the bottom of the <see cref="WiiBoard"/> and outside of the <see cref="Deadzone"/>; otherwise, false.
    /// </returns>
    public bool GetBottom()
    {
        return GetState().Y < 0 && !InDeadZone();
    }

    /// <summary>
    /// Determines whether the balancepoint is inside of the <see cref="Deadzone"/> or not.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the balancepoint is inside of the <see cref="Deadzone"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public bool InDeadZone()
    {
        return GetState().LengthSquared() < Deadzone*Deadzone;
    }

    /// <summary>
    /// Determines on which <see cref="Areas"/> of the <see cref="WiiBoard"/> the balancepoint lays.
    /// </summary>
    /// <returns>
    /// the <see cref="Areas"/> on which the balancepoint lays.
    /// </returns>
    public Areas GetAreas()
    {
        if (InDeadZone())
            return Areas.Center;

        Areas top = GetState().Y >= 0 ? Areas.Top : Areas.Bottom;
        Areas right = GetState().X >= 0 ? Areas.Right : Areas.Left;
        return top | right;
    }

    /// <summary>
    /// Determines whether the balancepoint is inside of the given area of the <see cref="WiiBoard"/> or not.
    /// </summary>
    /// <param name="area">
    /// An area of the <see cref="WiiBoard"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the balancepoint is inside of the given <paramref name="area"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentException"></exception>
    public bool InArea(Areas area)
    {
        return GetAreas().HasFlag(area);
    }
}

/// <summary>
/// An <see langword="enum"/> used to tell in which area of the <see cref="WiiBoard"/> the balancepoint is.<br/>
/// Should be checked against using <see cref="Enum.HasFlag(Enum)"/>.
/// </summary>
[Flags]
public enum Areas
{
    /// <summary>
    /// Left area of the board.
    /// </summary>
    Left = 1,
    /// <summary>
    /// Right area of the board.
    /// </summary>
    Right = 2,
    /// <summary>
    /// Top area of the board.
    /// </summary>
    Top = 4,
    /// <summary>
    /// Bottom area of the board.
    /// </summary>
    Bottom = 8,
    /// <summary>
    /// Center of the board, inside the deadzone.
    /// </summary>
    Center = 16
}
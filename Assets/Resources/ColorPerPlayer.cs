using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Basic script to assign a color per player in a PUN room.
/// </summary>
/// <remarks>
/// This script is but one possible implementation to have players select a color in a room.
/// It uses a Custom Property per player to store currently selected colors.
/// When a player joins and someone else didn't pick a color yet, this script waits.
/// When a color is selected or a player leaves, this scripts selects a color if it didn't do that before.
///
/// This could be extended to provide easy access to each player's color. Alternatively, you could write
/// extension methods for the PhotonPlayer class to access the Custom Property for colors in a seamless way.
/// See TeamExtensions for an example.
/// </remarks>
/// <author>
/// satoshi-maemoto
/// </author>

public class ColorPerPlayer : MonoBehaviourPunCallbacks {
  /// <summary>
  /// Defines the available colors per room. There should be at least one color per available player spot.
  /// </summary>
  public Color[] Colors = new Color[] { Color.red, Color.blue, Color.yellow, Color.green };

  /// <summary>
  /// Property-key for Player Color. the value will be the index of the player's color in array Colors (0...)
  /// </summary>
  public const string ColorProp = "pc";
  public const string RColor = "r";
  public const string GColor = "g";
  public const string BColor = "b";

  /// <summary>
  /// Color this player selected. Defaults to grey.
  /// </summary>
  public Color MyColor = Color.grey;

  public bool ColorPicked { get; set; }

  public override void OnJoinedRoom () {
    SelectColor ();
  }

  public override void OnDisconnected (DisconnectCause cause) {
    SelectColor ();
  }

  public override void OnPlayerPropertiesUpdate (Player targetPlayer, Hashtable changedProps) {
    // important: SelectColor() might cause a call to OnPhotonPlayerPropertiesChanged().
    // to avoid endless recursion (and a crash), we skip calling SelectColor() if this player changed props.
    // we could also check which props changed and skip all changes, aside from color-selection.
    //PhotonPlayer player = playerAndUpdatedProps[0] as PhotonPlayer;
    if (targetPlayer != null && targetPlayer.IsLocal) {
      return;
    }
    SelectColor ();
  }

  public override void OnLeftRoom () {
    // colors are select per room.
    Reset ();
  }

  /// <summary>
  /// Resets the color locally. In this class and the PhotonNetwork.player instance.
  /// </summary>
  public void Reset () {
    this.MyColor = Color.grey;
    ColorPicked = false;

    // colors are select per room. to reset, we have to clean the locally cached property in PhotonPlayer, too
    Hashtable colorProp = new Hashtable ();
    colorProp.Add (ColorProp, null);
    colorProp.Add (RColor, null);
    colorProp.Add (GColor, null);
    colorProp.Add (BColor, null);
    PhotonNetwork.LocalPlayer.SetCustomProperties (colorProp);
  }

  /// <summary>
  /// Attempts to select a color out of the existing, not-yet-taken ones.
  /// </summary>
  /// <remarks>
  /// Available colors are defined in Colors.
  /// Colors are taken, if their Colors index is in a player's Custom Property with the key ColorProp.
  ///
  /// </remarks>
  public void SelectColor () {
    if (ColorPicked) {
      return;
    }

    HashSet<int> takenColors = new HashSet<int> ();

    // check which colors the OTHERS picked. we pick one of the remaining colors.
    foreach (Player player in PhotonNetwork.PlayerListOthers) {
      if (player.CustomProperties.ContainsKey (ColorProp)) {
        int picked = (int) player.CustomProperties[ColorProp];
        Debug.Log ("Taken color index: " + picked);
        takenColors.Add (picked);
      } else {
        // a player joined earlier but didn't set a color yet. as that player has a lower ID, it should select a color before we do.
        // we will wait to avoid clashes when 2 players join soon after another. we don't want a color picked twice!
        if (player.ActorNumber < PhotonNetwork.LocalPlayer.ActorNumber) {
          Debug.Log ("Can't select a color yet. This player has to pick one first: " + player);
          return;
        }
      }
    }

    //Debug.Log("Taken colors: " + takenColors.Count);

    if (takenColors.Count == this.Colors.Length) {
      Debug.LogWarning ("No color available! All picked. Colors length should match MaxPlayers of the room.");
      return;
    }

    // go through the list of available colors and check each if it's taken or not
    // pick the first color that's not taken
    for (int index = 0; index < this.Colors.Length; index++) {
      if (!takenColors.Contains (index)) {
        Color color = this.Colors[index];
        this.MyColor = color;

        // this stores the picked color in the server and makes it known to the others (network sync)
        Hashtable colorProp = new Hashtable ();
        colorProp.Add (ColorProp, index);
        colorProp.Add (RColor, color.r);
        colorProp.Add (GColor, color.g);
        colorProp.Add (BColor, color.b);
        PhotonNetwork.LocalPlayer.SetCustomProperties (colorProp); // this goes to the server asap.

        Debug.Log ("Selected my color: " + this.MyColor);
        ColorPicked = true;
        break; // one color selected. break this loop.
      }
    }
  }
}
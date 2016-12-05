using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AwesomePirates.src.Networking
{
    class Networking
    {
        // 8kb/s is max bandwidth you can use to include 99% of players.

        // Use Reliable and InOrder for very important messages (like New Level: 3).

        // Don't use Reliable and InOrder for most things (slow).

        // Use XNA Network simulations (drops packets deliberately) to test your code.

        // Send fewer large packets rather than many small packets to reduce packet overhead.

        // Compress packets if you still need lower bandwidth.
            // e.g. Use bitfields instead of enums/bools
            // Convert float rotation into 0-255 (byte) range, then convert back on the other side.
            // Use domain-specific knowledge to reduce packet size (ie Spawn event 5 happened, rather than Rocket Launcher spawned at x,y,z).
            // Don't send strings.

        // Voice is automatic and is 0.5kb/s per player.
            // Use enable/send voice API if you have 16 players, which would otherwise eat up your entire bandwidth with just voice!
            // Allow everyone to speak in lobby (because there is no bandwidth wasted on game data in the lobby).

        // Mix peer-to-peer and client-server.
            // Peer-to-Peer
                // Movement. 
            // Client-Server
                // Can't have two ships get the same Abyssal pickup.
                // Pick one XBox to be in charge of who gets the pickup.
            
        // Don't try to make every XBox see exactly the same thing. Just make sure they all see the same important things (end of game, winner, etc)
        // and try to make everyone's XBox see similar state.

        // Interpolate between movement points. But also send more information so that receiver can extrapolate until the next packet.
            // I'm here (x,y) and I'm accelerating and turning with Z force.

        // Separate physics state from the rest of your object's state.
            // Have multiple Actor states in one actor.
                // i. Where am I drawing it on the screen.
                // ii. Where was it for sure the last time I received a network packet.
                // iii. Where do I currently predict it's going to be based on the current network packet.
                // iv. What's the previous prediction I was making before I got the latest packet, so I can smooth from that position to the new position.
                // v. Now you can just run your physics update method and ignore much of the complexity.
                // See sample on XNA Creators website.
                    // Can toggle prediction/smoothing on/off and can alter latency simulation to test different network conditions.

        // Determine certain things based on information relative to local client. So if local client shoots at an angle that looks like he will miss by 5 degrees to the left,
        // send a packet that says they will miss 5 degrees to the left, rather than sending something that missed on the sending client, but hits on the receiving client.
    }
}

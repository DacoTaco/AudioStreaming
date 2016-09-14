# AudioStreaming

NOTE : this is silly and needs to be reworked. it doesn't even have a api lol

an app i created to stream my music to a device without having the music there. also a project i made to get my hands dirty with C#. this is nowhere near clean code though

the project uses:

.NET 4.5 : because C#

Naudio : audio backbone

Lz4.Net/Lz4 : compression of the packets.



TODO : 

- encode the raw PCM from capture to MP3 and then compress with Lz4
- implement settings
- redesign alot of the code. its REALLY shitty designed

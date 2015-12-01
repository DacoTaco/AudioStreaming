# AudioStreaming
an app i created to stream my music to a device without having the music there. also a project i made to get my hands dirty with C#

the project uses:

.NET 4.5 : because C#

Naudio : audio backbone

Lz4.Net/Lz4 : compression of the packets.



TODO : 

- encode the raw PCM from capture to MP3 and then compress with Lz4
- manage the buffer better. dont play data when its low etc etc

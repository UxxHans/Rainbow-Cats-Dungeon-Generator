# Rainbow Cats Dungeon Generator
A procedural dungeon generator using maze algorithm.

Hi there!
I made a dungeon generator for fun using C# and Unity. You can control the camera and character, toggle different layers in the generator.

I was inspired by Bob Nystrom's dungeon generation idea. Unfortunately, the code in his repository is neither C# nor useable for Unity. Therefore, I have to start my own project in Unity from scratch. I watched Javidx9's maze algorithm introduction and also a few articles about procedural generation, FOFO, and LIFO and finally developed my own system to generate dungeon in Unity.

I was stuck by the sorting problem caused by the top-down walls, but thankfully it was solved by using foreground and background layers. After reading this forum: https://forum.unity.com/threads/is-it-possibly-to-sort-sprites-along-tilemap-wit...

Sadly I could not get the 2d shadow caster working in the tilemap, as the maze or dungeon is a hollow shape, which makes this solution no longer works for me: https://forum.unity.com/threads/the-new-2d-lighting-on-tilemaps-advice-and-sugge...

Here is the link to my itch.io page:
https://rainbow-cats-studio.itch.io/rainbow-cats-dungeon-generator

![alt text](https://github.com/UxxHans/Rainbow-Cats-Dungeon-Generator/blob/main/Pictures/1.png)
![alt text](https://github.com/UxxHans/Rainbow-Cats-Dungeon-Generator/blob/main/Pictures/2.png)
![alt text](https://github.com/UxxHans/Rainbow-Cats-Dungeon-Generator/blob/main/Pictures/3.png)

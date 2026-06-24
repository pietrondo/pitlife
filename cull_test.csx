using System;

int pw = 3200;
int ph = 2400;
int camX = 0;
int camY = 0;
int camW = 1280;
int camH = 800;

for (int dy = -1; dy <= 1; dy++) {
    for (int dx = -1; dx <= 1; dx++) {
        int x = dx * pw;
        int y = dy * ph;
        if (x < camX + camW && x + pw > camX &&
            y < camY + camH && y + ph > camY) {
            Console.WriteLine($"Intersects: dx={dx}, dy={dy}");
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Rendering;

public class Rooms
{
    public BoundsInt body;
    public string type;
    public bool rightConnected;
    public bool leftConnected;
    public bool upConnected;
    public bool downConnected;

    public Vector2 position;
    public Rooms (BoundsInt body, Vector2 position)
    {
        this.body = body;
        this.position = position;
    }

    public void setType(string type)
    {
        this.type = type;
        
    }

    public List<int> availableDirection()
    {
        List<int> direction = new List<int>();

        if(!upConnected)
        {
            direction.Add(3);
        }
        if(!downConnected)
        {
            direction.Add(4);
        }
        if(!leftConnected)
        {
            direction.Add(2);
        }
        if(!rightConnected)
        {
            direction.Add(1);
        }
        return direction;

    }
    public Rooms Branch(int direction)
    {
        Rooms nextRoom;
        int xsize = Random.Range(5, 10)*2+1;
        if(xsize % 2 == 0)
        {
            xsize++;
        }
        int ysize = Random.Range(5, 10)*2+1;
        if(ysize % 2 == 0)
        {
            ysize++;
        }
        int xdif = (this.body.size.x - xsize)/2;
        int ydif = (this.body.size.y - ysize)/2;
        switch (direction)
        {
            case 1:
                nextRoom = new Rooms(new BoundsInt(this.body.xMax+20, this.body.yMin+ydif, -10, xsize, ysize, 20), this.position+new Vector2(1, 0));
                nextRoom.setConnected(2);
                rightConnected = true;
                break;
            case 2:
                nextRoom = new Rooms(new BoundsInt(this.body.xMin-20-xsize, this.body.yMin+ydif, -10, xsize, ysize, 20), this.position+new Vector2(-1, 0));
                nextRoom.setConnected(1);
                leftConnected = true;
                break;
            case 3:
                nextRoom = new Rooms(new BoundsInt(this.body.xMin+xdif, this.body.yMax+20, -10, xsize, ysize, 20), this.position+new Vector2(0, 1));
                nextRoom.setConnected(4);
                upConnected = true;
                break;
            case 4:
                nextRoom = new Rooms(new BoundsInt(this.body.xMin+xdif, this.body.yMin-20-ysize, -10, xsize, ysize, 20), this.position+new Vector2(0, -1));
                nextRoom.setConnected(3);
                downConnected = true;
                break;
            default:
                nextRoom = new Rooms(new BoundsInt(this.body.xMin, this.body.yMin-20, -10, 20, 20, 20), this.position+new Vector2(1, 0));
                nextRoom.setConnected(3);
                break;
        }
        return nextRoom;
    }

    public void setConnected(int direction, bool yes = true)
    {
        switch (direction)
        {
            case 1:
                rightConnected = yes;
                break;
            case 2:
                leftConnected = yes;
                break;
            case 3:
                upConnected = yes;
                break;
            case 4:
                downConnected = yes;
                break;
        }
    }

    

}
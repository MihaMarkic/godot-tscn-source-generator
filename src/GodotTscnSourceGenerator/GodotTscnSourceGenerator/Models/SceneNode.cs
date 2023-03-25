﻿using System;
using System.Collections.Generic;

namespace GodotTscnSourceGenerator.Models
{
    internal class SceneNode
    {
        public string Segment { get; }
        public List<string> Scenes { get; } = new List<string>();
        public Dictionary<string, SceneNode> Nodes { get; } = new Dictionary<string, SceneNode>(StringComparer.OrdinalIgnoreCase);
        public SceneNode(string segment)
        {
            Segment = segment;
        }
    }
}

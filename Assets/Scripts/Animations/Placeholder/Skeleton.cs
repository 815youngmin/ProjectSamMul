#nullable enable
using UnityEngine;

namespace Z.Animations.Placeholder
{
    /// <summary>
    /// Placeholder bone. Every bone hangs directly off the root; its world position is the local
    /// position scaled by the skeleton's ScaleX/ScaleY, which is what the ported aiming code expects.
    /// </summary>
    public class Bone
    {
        public string Name { get; }
        public Skeleton Skeleton { get; }
        public Bone? Parent { get; }

        public float X { get; set; }
        public float Y { get; set; }
        public float Rotation { get; set; }
        public float ScaleX { get; set; } = 1f;
        public float ScaleY { get; set; } = 1f;
        public float WorldX { get; private set; }
        public float WorldY { get; private set; }

        public Bone(string name, Skeleton skeleton, Bone? parent)
        {
            Name = name;
            Skeleton = skeleton;
            Parent = parent;
        }

        public void SetToSetupPose()
        {
            X = 0f;
            Y = 0f;
            Rotation = 0f;
            ScaleX = 1f;
            ScaleY = 1f;
        }

        public void UpdateWorldTransform()
        {
            WorldX = X * Skeleton.ScaleX;
            WorldY = Y * Skeleton.ScaleY;
        }

        /// <summary>Kept for API parity; the placeholder has no applied/pose split.</summary>
        public void UpdateAppliedTransform()
        {
            UpdateWorldTransform();
        }

        public Vector2 GetLocalPosition() => new Vector2(X, Y);

        public void SetLocalPosition(Vector2 position)
        {
            X = position.x;
            Y = position.y;
        }

        public Vector2 GetWorldPosition(Transform skeletonTransform)
        {
            UpdateWorldTransform();
            return skeletonTransform.TransformPoint(new Vector3(WorldX, WorldY, 0f));
        }

        public override string ToString() => Name;
    }

    public class Slot
    {
        public string Name { get; }
        public Skeleton Skeleton { get; }
        public Bone Bone { get; }
        public Attachment? Attachment { get; set; }

        public Slot(string name, Skeleton skeleton, Bone bone)
        {
            Name = name;
            Skeleton = skeleton;
            Bone = bone;
        }

        public void SetToSetupPose()
        {
            Attachment = null;
        }

        public override string ToString() => Name;
    }

    public class Skeleton
    {
        public SkeletonData Data { get; }
        public ExposedList<Bone> Bones { get; } = new ExposedList<Bone>();
        public ExposedList<Slot> Slots { get; } = new ExposedList<Slot>();
        public Bone RootBone { get; }
        public Skin? Skin { get; private set; }

        public float X { get; set; }
        public float Y { get; set; }
        /// <summary>Sign flips the body horizontally (positive faces left, matching the original assets).</summary>
        public float ScaleX { get; set; } = 1f;
        public float ScaleY { get; set; } = 1f;
        public float R { get; set; } = 1f;
        public float G { get; set; } = 1f;
        public float B { get; set; } = 1f;
        public float A { get; set; } = 1f;

        public Skeleton(SkeletonData data)
        {
            Data = data;
            RootBone = new Bone("root", this, null);
            Bones.Add(RootBone);
            foreach (var boneName in data.Bones)
            {
                Bones.Add(new Bone(boneName, this, RootBone));
            }
            Skin = data.DefaultSkin;
        }

        public Color GetColor() => new Color(R, G, B, A);

        public void SetColor(Color color)
        {
            R = color.r;
            G = color.g;
            B = color.b;
            A = color.a;
        }

        public Vector2 GetLocalScale() => new Vector2(ScaleX, ScaleY);

        public Bone? FindBone(string? boneName)
        {
            if (string.IsNullOrEmpty(boneName))
            {
                return null;
            }

            foreach (var bone in Bones)
            {
                if (bone.Name == boneName)
                {
                    return bone;
                }
            }

            var created = new Bone(boneName!, this, RootBone);
            Bones.Add(created);
            return created;
        }

        public Slot? FindSlot(string? slotName)
        {
            if (string.IsNullOrEmpty(slotName))
            {
                return null;
            }

            foreach (var slot in Slots)
            {
                if (slot.Name == slotName)
                {
                    return slot;
                }
            }

            var created = new Slot(slotName!, this, RootBone);
            Slots.Add(created);
            return created;
        }

        public void SetSkin(Skin? newSkin)
        {
            Skin = newSkin;
        }

        public void SetSkin(string skinName)
        {
            Skin = Data.FindSkin(skinName);
        }

        public void SetToSetupPose()
        {
            SetBonesToSetupPose();
            SetSlotsToSetupPose();
        }

        public void SetBonesToSetupPose()
        {
            foreach (var bone in Bones)
            {
                bone.SetToSetupPose();
            }
        }

        public void SetSlotsToSetupPose()
        {
            foreach (var slot in Slots)
            {
                slot.SetToSetupPose();
            }
        }

        public void UpdateWorldTransform()
        {
            foreach (var bone in Bones)
            {
                bone.UpdateWorldTransform();
            }
        }
    }
}

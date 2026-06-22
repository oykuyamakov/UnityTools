using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace _App.Scripts.Utility.UI
{
    [AddComponentMenu("Layout/Better Grid Layout Group", 152)]
    public class BetterGridLayoutGroup : LayoutGroup
    {
        public enum Corner
        {
            UpperLeft = 0,
            UpperRight = 1,
            LowerLeft = 2,
            LowerRight = 3
        }

        public enum Axis
        {
            Horizontal = 0,
            Vertical = 1
        }

        public enum Constraint
        {
            Flexible = 0,
            FixedColumnCount = 1,
            FixedRowCount = 2
        }

        public enum IncompleteRowAlignment
        {
            Default, 
            Center, 
            End 
        }

        [SerializeField]
        private Corner m_StartCorner = Corner.UpperLeft;

        public Corner StartCorner
        {
            get => m_StartCorner;
            set => SetProperty(ref m_StartCorner, value);
        }

        [SerializeField]
        private Axis m_StartAxis = Axis.Horizontal;

        public Axis StartAxis
        {
            get => m_StartAxis;
            set => SetProperty(ref m_StartAxis, value);
        }

        [SerializeField]
        private Vector2 m_CellSize = new Vector2(100, 100);

        public Vector2 CellSize
        {
            get => m_CellSize;
            set => SetProperty(ref m_CellSize, value);
        }

        [SerializeField]
        private Vector2 m_Spacing = Vector2.zero;

        public Vector2 Spacing
        {
            get => m_Spacing;
            set => SetProperty(ref m_Spacing, value);
        }

        [SerializeField]
        private Constraint m_GridConstraint = Constraint.Flexible;

        public Constraint GridConstraint
        {
            get => m_GridConstraint;
            set => SetProperty(ref m_GridConstraint, value);
        }

        [SerializeField][ShowIf("m_GridConstraint",  Constraint.FixedColumnCount), ShowIf("m_GridConstraint", Constraint.FixedRowCount)]
        private int m_ConstraintCount = 2;

        public int ConstraintCount
        {
            get => m_ConstraintCount;
            set => SetProperty(ref m_ConstraintCount, Mathf.Max(1, value));
        }

        [SerializeField]
        private IncompleteRowAlignment m_IncompleteRowAlignment = IncompleteRowAlignment.Default;

        public IncompleteRowAlignment IncompleteRowAlign
        {
            get => m_IncompleteRowAlignment;
            set => SetProperty(ref m_IncompleteRowAlignment, value);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            ConstraintCount = ConstraintCount;
        }
#endif

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();

            int minColumns = 0;
            int preferredColumns = 0;

            if (m_GridConstraint == Constraint.FixedColumnCount)
            {
                minColumns = preferredColumns = m_ConstraintCount;
            }
            else if (m_GridConstraint == Constraint.FixedRowCount)
            {
                minColumns = preferredColumns = Mathf.CeilToInt(rectChildren.Count / (float)m_ConstraintCount - 0.001f);
            }
            else
            {
                minColumns = 1;
                preferredColumns = Mathf.CeilToInt(Mathf.Sqrt(rectChildren.Count));
            }

            SetLayoutInputForAxis(
                padding.horizontal + (CellSize.x + Spacing.x) * minColumns - Spacing.x,
                padding.horizontal + (CellSize.x + Spacing.x) * preferredColumns - Spacing.x,
                -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            int minRows = 0;

            if (m_GridConstraint == Constraint.FixedColumnCount)
            {
                minRows = Mathf.CeilToInt(rectChildren.Count / (float)m_ConstraintCount - 0.001f);
            }
            else if (m_GridConstraint == Constraint.FixedRowCount)
            {
                minRows = m_ConstraintCount;
            }
            else
            {
                float width = rectTransform.rect.width;
                int cellCountX = Mathf.Max(1,
                    Mathf.FloorToInt((width - padding.horizontal + Spacing.x + 0.001f) / (CellSize.x + Spacing.x)));
                minRows = Mathf.CeilToInt(rectChildren.Count / (float)cellCountX);
            }

            float minSpace = padding.vertical + (CellSize.y + Spacing.y) * minRows - Spacing.y;
            SetLayoutInputForAxis(minSpace, minSpace, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            SetCellsAlongAxis(0);
        }

        public override void SetLayoutVertical()
        {
            SetCellsAlongAxis(1);
        }

        private void SetCellsAlongAxis(int axis)
        {
            int rectChildrenCount = rectChildren.Count;

            if (axis == 0)
            {
                for (int i = 0; i < rectChildrenCount; i++)
                {
                    RectTransform rect = rectChildren[i];

                    m_Tracker.Add(this, rect,
                        DrivenTransformProperties.Anchors |
                        DrivenTransformProperties.AnchoredPosition |
                        DrivenTransformProperties.SizeDelta);

                    rect.anchorMin = Vector2.up;
                    rect.anchorMax = Vector2.up;
                    rect.sizeDelta = CellSize;
                }

                return;
            }

            float width = rectTransform.rect.size.x;
            float height = rectTransform.rect.size.y;

            int cellCountX = 1;
            int cellCountY = 1;

            if (m_GridConstraint == Constraint.FixedColumnCount)
            {
                cellCountX = m_ConstraintCount;

                if (rectChildrenCount > cellCountX)
                {
                    cellCountY = rectChildrenCount / cellCountX;

                    if (rectChildrenCount % cellCountX > 0)
                    {
                        cellCountY += 1;
                    }
                }
            }
            else if (m_GridConstraint == Constraint.FixedRowCount)
            {
                cellCountY = m_ConstraintCount;

                if (rectChildrenCount > cellCountY)
                {
                    cellCountX = rectChildrenCount / cellCountY;

                    if (rectChildrenCount % cellCountY > 0)
                    {
                        cellCountX += 1;
                    }
                }
            }
            else
            {
                if (CellSize.x + Spacing.x <= 0)
                {
                    cellCountX = int.MaxValue;
                }
                else
                {
                    cellCountX = Mathf.Max(1,
                        Mathf.FloorToInt((width - padding.horizontal + Spacing.x + 0.001f) / (CellSize.x + Spacing.x)));
                }

                if (CellSize.y + Spacing.y <= 0)
                {
                    cellCountY = int.MaxValue;
                }
                else
                {
                    cellCountY = Mathf.Max(1,
                        Mathf.FloorToInt((height - padding.vertical + Spacing.y + 0.001f) / (CellSize.y + Spacing.y)));
                }
            }

            int cornerX = (int)m_StartCorner % 2;
            int cornerY = (int)m_StartCorner / 2;

            int cellsPerMainAxis;
            int actualCellCountX;
            int actualCellCountY;

            if (m_StartAxis == Axis.Horizontal)
            {
                cellsPerMainAxis = cellCountX;
                actualCellCountX = Mathf.Clamp(cellCountX, 1, rectChildrenCount);

                if (m_GridConstraint == Constraint.FixedRowCount)
                {
                    actualCellCountY = Mathf.Min(cellCountY, rectChildrenCount);
                }
                else
                {
                    actualCellCountY = Mathf.Clamp(cellCountY, 1,
                        Mathf.CeilToInt(rectChildrenCount / (float)cellsPerMainAxis));
                }
            }
            else
            {
                cellsPerMainAxis = cellCountY;
                actualCellCountY = Mathf.Clamp(cellCountY, 1, rectChildrenCount);

                if (m_GridConstraint == Constraint.FixedColumnCount)
                {
                    actualCellCountX = Mathf.Min(cellCountX, rectChildrenCount);
                }
                else
                {
                    actualCellCountX = Mathf.Clamp(cellCountX, 1,
                        Mathf.CeilToInt(rectChildrenCount / (float)cellsPerMainAxis));
                }
            }

            Vector2 requiredSpace = new Vector2(
                actualCellCountX * CellSize.x + (actualCellCountX - 1) * Spacing.x,
                actualCellCountY * CellSize.y + (actualCellCountY - 1) * Spacing.y
            );

            Vector2 startOffset = new Vector2(
                GetStartOffset(0, requiredSpace.x),
                GetStartOffset(1, requiredSpace.y)
            );

            // --- NEW: last-row alignment data (Horizontal fill only) ---
            int lastRowIndex = -1;
            int itemsInLastRow = 0;

            if (m_StartAxis == Axis.Horizontal)
            {
                int rows = Mathf.CeilToInt(rectChildrenCount / (float)cellsPerMainAxis);
                lastRowIndex = rows - 1;

                itemsInLastRow = rectChildrenCount - lastRowIndex * cellsPerMainAxis;
                if (itemsInLastRow <= 0)
                {
                    itemsInLastRow = cellsPerMainAxis;
                }
            }

            for (int i = 0; i < rectChildrenCount; i++)
            {
                int positionX;
                int positionY;

                if (m_StartAxis == Axis.Horizontal)
                {
                    positionX = i % cellsPerMainAxis;
                    positionY = i / cellsPerMainAxis;
                }
                else
                {
                    positionX = i / cellsPerMainAxis;
                    positionY = i % cellsPerMainAxis;
                }

                if (cornerX == 1)
                {
                    positionX = actualCellCountX - 1 - positionX;
                }

                if (cornerY == 1)
                {
                    positionY = actualCellCountY - 1 - positionY;
                }

                float extraX = 0f;

                if (m_StartAxis == Axis.Horizontal &&
                    m_IncompleteRowAlignment != IncompleteRowAlignment.Default &&
                    positionY == lastRowIndex &&
                    itemsInLastRow < cellsPerMainAxis)
                {
                    int emptySlots = cellsPerMainAxis - itemsInLastRow;
                    float stepX = CellSize.x + Spacing.x;

                    if (m_IncompleteRowAlignment == IncompleteRowAlignment.Center)
                    {
                        extraX = emptySlots * 0.5f * stepX;
                    }
                    else if (m_IncompleteRowAlignment == IncompleteRowAlignment.End)
                    {
                        extraX = emptySlots * stepX;
                    }

                    if (cornerX == 1)
                    {
                        extraX = -extraX;
                    }
                }

                SetChildAlongAxis(rectChildren[i], 0, startOffset.x + extraX + (CellSize.x + Spacing.x) * positionX,
                    CellSize.x);
                SetChildAlongAxis(rectChildren[i], 1, startOffset.y + (CellSize.y + Spacing.y) * positionY, CellSize.y);
            }
        }
    }
}
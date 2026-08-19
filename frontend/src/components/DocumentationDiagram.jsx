import { useMemo } from "react";

export default function DocumentationDiagram({ diagram }) {
  const { nodes, edges } = diagram || {};

  const layout = useMemo(() => {
    const safeNodes = Array.isArray(nodes) ? nodes : [];
    const safeEdges = Array.isArray(edges) ? edges : [];

    const nodesById = new Map(
      safeNodes.map((node) => [node.id, node])
    );

    // Nombre d'entrées de chaque nœud.
    const incomingCount = new Map(safeNodes.map((node) => [node.id, 0]));

    safeEdges.forEach((edge) => {
      if (incomingCount.has(edge.targetId)) {
        incomingCount.set(
          edge.targetId,
          incomingCount.get(edge.targetId) + 1
        );
      }
    });

    // Nœuds de départ : aucun lien entrant.
    const roots = safeNodes.filter(
      (node) => incomingCount.get(node.id) === 0
    );

    const levels = [];
    const assignedLevels = new Map();
    const queue = [];

    roots.forEach((node) => {
      assignedLevels.set(node.id, 0);
      queue.push(node.id);
    });

    // Si aucun nœud de départ n'est détecté, on commence
    // avec le premier nœud.
    if (queue.length === 0 && safeNodes.length > 0) {
      assignedLevels.set(safeNodes[0].id, 0);
      queue.push(safeNodes[0].id);
    }

    // Détermine le niveau de chaque nœud selon ses relations.
    while (queue.length > 0) {
      const currentId = queue.shift();
      const currentLevel = assignedLevels.get(currentId) || 0;

      safeEdges
        .filter((edge) => edge.sourceId === currentId)
        .forEach((edge) => {
          if (!nodesById.has(edge.targetId)) return;

          const nextLevel = currentLevel + 1;
          const existingLevel = assignedLevels.get(edge.targetId);

          if (
            existingLevel === undefined ||
            nextLevel > existingLevel
          ) {
            assignedLevels.set(edge.targetId, nextLevel);
            queue.push(edge.targetId);
          }
        });
    }

    // Les éventuels nœuds non reliés sont placés à la fin.
    safeNodes.forEach((node) => {
      if (!assignedLevels.has(node.id)) {
        assignedLevels.set(node.id, assignedLevels.size > 0 ? Math.max(...assignedLevels.values()) + 1 : 0);
      }
    });

    nodes.forEach((node) => {
      const level = assignedLevels.get(node.id) || 0;

      if (!levels[level]) {
        levels[level] = [];
      }

      levels[level].push(node);
    });

    const nodeWidth = 220;
    const nodeHeight = 90;
    const horizontalGap = 70;
    const verticalGap = 100;
    const padding = 60;

    const maxNodesInLevel = Math.max(
      ...levels.map((level) => level?.length || 1)
    );

    const width = Math.max(
      700,
      maxNodesInLevel * nodeWidth +
        Math.max(0, maxNodesInLevel - 1) * horizontalGap +
        padding * 2
    );

    const height =
      levels.length * nodeHeight +
      Math.max(0, levels.length - 1) * verticalGap +
      padding * 2;

    const positions = new Map();

    levels.forEach((levelNodes, levelIndex) => {
      if (!levelNodes?.length) return;

      const totalWidth =
        levelNodes.length * nodeWidth +
        Math.max(0, levelNodes.length - 1) * horizontalGap;

      const startX = (width - totalWidth) / 2;

      levelNodes.forEach((node, index) => {
        positions.set(node.id, {
          x: startX + index * (nodeWidth + horizontalGap),
          y: padding + levelIndex * (nodeHeight + verticalGap),
        });
      });
    });

    return {
      safeEdges,
      nodesById,
      positions,
      width,
      height,
      nodeWidth,
      nodeHeight,
    };
  }, [nodes, edges]);

  if (!Array.isArray(nodes) || nodes.length === 0) {
    return (
      <div style={{ color: "var(--color-muted)", fontSize: 13 }}>
        Aucun diagramme disponible pour cette documentation.
      </div>
    );
  }

  const getNodeTypeLabel = (node) =>
    node.type || node.nodeType || "Non défini";

  return (
    <div
      style={{
        overflowX: "auto",
        padding: "var(--space-4)",
        border: "1px solid var(--color-border)",
        borderRadius: "var(--radius-sm)",
        background: "var(--color-surface-alt)",
      }}
    >
      <div
        style={{
          marginBottom: 12,
          fontSize: 12,
          color: "var(--color-muted)",
        }}
      >
        Représentation technique des relations entre les éléments du flux.
      </div>

      <svg
        width={layout.width}
        height={layout.height}
        viewBox={`0 0 ${layout.width} ${layout.height}`}
        role="img"
        aria-label="Diagramme du flux Power Automate"
      >
        <defs>
          <marker
            id="arrow"
            markerWidth="10"
            markerHeight="10"
            refX="9"
            refY="3"
            orient="auto"
            markerUnits="strokeWidth"
          >
            <path d="M0,0 L0,6 L9,3 z" fill="currentColor" />
          </marker>
        </defs>

        {/* Connexions entre les nœuds */}
        {layout.safeEdges.map((edge, index) => {
          const source = layout.positions.get(edge.sourceId);
          const target = layout.positions.get(edge.targetId);

          if (!source || !target) return null;

          const startX = source.x + layout.nodeWidth / 2;
          const startY = source.y + layout.nodeHeight;

          const endX = target.x + layout.nodeWidth / 2;
          const endY = target.y;

          const middleY = (startY + endY) / 2;

          return (
            <g key={`${edge.sourceId}-${edge.targetId}-${index}`}>
              <path
                d={`
                  M ${startX} ${startY}
                  C ${startX} ${middleY},
                    ${endX} ${middleY},
                    ${endX} ${endY}
                `}
                fill="none"
                stroke="currentColor"
                strokeWidth="1.5"
                markerEnd="url(#arrow)"
              />

              {edge.label && (
                <text
                  x={(startX + endX) / 2}
                  y={middleY - 6}
                  textAnchor="middle"
                  fontSize="12"
                  fontWeight="600"
                  fill="currentColor"
                >
                  {edge.label}
                </text>
              )}
            </g>
          );
        })}

        {/* Nœuds */}
        {nodes.map((node) => {
          const position = layout.positions.get(node.id);

          if (!position) return null;

          return (
            <g key={node.id}>
              <rect
                x={position.x}
                y={position.y}
                width={layout.nodeWidth}
                height={layout.nodeHeight}
                rx="10"
                ry="10"
                fill="var(--color-surface)"
                stroke="var(--color-border)"
                strokeWidth="1.5"
              />

              <text
                x={position.x + layout.nodeWidth / 2}
                y={position.y + 35}
                textAnchor="middle"
                fontSize="14"
                fontWeight="700"
                fill="currentColor"
              >
                {node.name}
              </text>

              <text
                x={position.x + layout.nodeWidth / 2}
                y={position.y + 60}
                textAnchor="middle"
                fontSize="11"
                fill="var(--color-muted)"
              >
                Type : {getNodeTypeLabel(node)}
              </text>
            </g>
          );
        })}
      </svg>
    </div>
  );
}
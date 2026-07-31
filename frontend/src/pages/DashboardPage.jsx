import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";

import {
  FileText,
  Workflow,
  PenLine,
  CheckCircle2,
  Archive,
  RefreshCw,
  Sparkles,
  ArrowRight
} from "lucide-react";

import { getDashboardStats } from "../services/dashboardService";

import PageHeader from "../components/ui/PageHeader";
import StatusBadge from "../components/ui/StatusBadge";
import EmptyState from "../components/ui/EmptyState";
import Spinner from "../components/ui/Spinner";
import Button from "../components/ui/Button";

const STAT_CARDS = [
  {
    key: "totalDocumentations",
    label: "Documentations",
    icon: FileText,
    color: "var(--color-primary)"
  },
  {
    key: "totalFlowsImported",
    label: "Flux importés",
    icon: Workflow,
    color: "#00BCF2"
  },
  {
    key: "draftCount",
    label: "Brouillons",
    icon: PenLine,
    color: "#F59E0B"
  },
  {
    key: "validatedCount",
    label: "Validées",
    icon: CheckCircle2,
    color: "#16A34A"
  },
  {
    key: "archivedCount",
    label: "Archivées",
    icon: Archive,
    color: "#6B7280"
  }
];

export default function DashboardPage() {

  const [stats, setStats] = useState(null);

  const [isLoading, setIsLoading] = useState(true);

  const [isRefreshing, setIsRefreshing] = useState(false);

  const [error, setError] = useState(null);

  const loadStats = useCallback((showSpinner) => {

    if (showSpinner)
      setIsLoading(true);
    else
      setIsRefreshing(true);

    setError(null);

    return getDashboardStats()

      .then(setStats)

      .catch(() =>
        setError(
          "Impossible de charger les statistiques."
        )
      )

      .finally(() => {

        setIsLoading(false);

        setIsRefreshing(false);

      });

  }, []);

  useEffect(() => {

    loadStats(true);

  }, [loadStats]);

  return (

    <div className="page-content">

      <PageHeader

        eyebrow="Microsoft Power Automate"

        title="Tableau de bord"

        description="Visualisez rapidement l'activité de votre plateforme, importez un flux Power Automate et générez automatiquement une documentation grâce à l'intelligence artificielle."

        actions={
          <>
            <Button
              variant="secondary"
              onClick={() => loadStats(false)}
              disabled={isRefreshing}
            >
              <RefreshCw size={16} />

              {isRefreshing
                ? "Actualisation..."
                : "Actualiser"}
            </Button>

            <Link to="/importer">

              <Button>

                <Workflow size={16} />

                Importer un flux

              </Button>

            </Link>

          </>
        }

      />

      {/* HERO */}

      <div
        className="card"
        style={{

          marginBottom: "32px",

          overflow: "hidden",

          background:
            "linear-gradient(135deg,#0078D4,#00BCF2)",

          color: "white",

          border: "none",

          boxShadow:
            "0 14px 40px rgba(0,120,212,.18)"
        }}
      >

        <div
          style={{

            padding: "42px"

          }}
        >

          <div
            style={{

              display: "flex",

              justifyContent: "space-between",

              alignItems: "center",

              flexWrap: "wrap",

              gap: 24

            }}
          >

            <div
              style={{

                maxWidth: 650

              }}
            >

              <div
                style={{

                  display: "inline-flex",

                  alignItems: "center",

                  gap: 8,

                  background: "rgba(255,255,255,.18)",

                  padding: "6px 14px",

                  borderRadius: 999,

                  marginBottom: 18

                }}
              >

                <Sparkles size={16} />

                Intelligence artificielle

              </div>

              <h1
                style={{

                  color: "white",

                  marginBottom: 16

                }}
              >

                Générez automatiquement la documentation de vos flux Power Automate

              </h1>

              <p
                style={{

                  fontSize: 16,

                  opacity: .95,

                  lineHeight: 1.7,

                  maxWidth: 580

                }}
              >

                Importez un fichier JSON,
                laissez l'IA générer une documentation complète,
                puis modifiez, sauvegardez et exportez le résultat.

              </p>

              <div
                style={{

                  display: "flex",

                  gap: 16,

                  marginTop: 28,

                  flexWrap: "wrap"

                }}
              >

                <Link to="/importer">

                  <Button
                    style={{

                      background: "white",

                      color: "var(--color-primary)",

                      border: "none"

                    }}
                  >

                    Importer maintenant

                    <ArrowRight size={16} />

                  </Button>

                </Link>

              </div>

            </div>

            <Workflow
              size={150}
              style={{

                opacity: .12

              }}
            />

          </div>

        </div>

      </div>
            {isLoading && (
        <div
          className="row"
          style={{
            justifyContent: "center",
            padding: "60px",
            color: "var(--color-muted)"
          }}
        >
          <Spinner />

          <span style={{ marginLeft: 12 }}>
            Chargement du tableau de bord...
          </span>
        </div>
      )}

      {error && !isLoading && (
        <div
          className="card"
          style={{
            padding: "20px",
            borderLeft: "4px solid var(--color-danger)",
            marginBottom: 30
          }}
        >
          <p style={{ color: "var(--color-danger)" }}>
            {error}
          </p>
        </div>
      )}

      {stats && !isLoading && (
        <>

          {/* ==============================
                 CARTES STATISTIQUES
          ============================== */}

          <div
            style={{
              display: "grid",
              gridTemplateColumns:
                "repeat(auto-fit,minmax(220px,1fr))",
              gap: 22,
              marginBottom: 34
            }}
          >
            {STAT_CARDS.map(
              ({ key, label, icon: Icon, color }) => (

                <div
                  key={key}
                  className="card"
                  style={{
                    padding: 24,
                    position: "relative",
                    overflow: "hidden"
                  }}
                >

                  {/* Barre colorée supérieure */}

                  <div
                    style={{
                      position: "absolute",
                      top: 0,
                      left: 0,
                      right: 0,
                      height: 5,
                      background: color
                    }}
                  />

                  <div
                    className="row"
                    style={{
                      justifyContent: "space-between",
                      marginBottom: 18
                    }}
                  >

                    <div>

                      <div
                        style={{
                          color: "var(--color-muted)",
                          fontSize: 13,
                          fontWeight: 600,
                          marginBottom: 8
                        }}
                      >
                        {label}
                      </div>

                      <div
                        style={{
                          fontSize: 36,
                          fontWeight: 700,
                          color: "var(--color-ink)"
                        }}
                      >
                        {stats[key]}
                      </div>

                    </div>

                    <div
                      style={{
                        width: 56,
                        height: 56,
                        borderRadius: 16,
                        background: "var(--color-primary-soft)",
                        display: "flex",
                        justifyContent: "center",
                        alignItems: "center"
                      }}
                    >
                      <Icon
                        size={28}
                        color={color}
                      />
                    </div>

                  </div>

                  <div
                    style={{
                      marginTop: 16,
                      display: "flex",
                      alignItems: "center",
                      gap: 8,
                      color: "var(--color-muted)",
                      fontSize: 13
                    }}
                  >
                    <div
                      style={{
                        width: 8,
                        height: 8,
                        borderRadius: "50%",
                        background: color
                      }}
                    />

                    Données en temps réel

                  </div>

                </div>

              )
            )}
          </div>

          {/* ==============================
                ACTIONS RAPIDES
          ============================== */}

          <div
            className="card"
            style={{
              padding: 30,
              marginBottom: 34
            }}
          >

            <div
              className="row"
              style={{
                justifyContent: "space-between",
                marginBottom: 24
              }}
            >

              <div>

                <h2>
                  Actions rapides
                </h2>

                <p
                  style={{
                    color: "var(--color-muted)",
                    marginTop: 6
                  }}
                >
                  Accédez rapidement aux principales fonctionnalités.
                </p>

              </div>

            </div>

            <div
              style={{
                display: "grid",
                gridTemplateColumns:
                  "repeat(auto-fit,minmax(220px,1fr))",
                gap: 20
              }}
            >

              <Link
                to="/importer"
                style={{ textDecoration: "none" }}
              >

                <div
                  className="card"
                  style={{
                    padding: 24,
                    cursor: "pointer",
                    height: "100%"
                  }}
                >

                  <Workflow
                    color="var(--color-primary)"
                    size={34}
                  />

                  <h3
                    style={{
                      marginTop: 18,
                      marginBottom: 8
                    }}
                  >
                    Importer un flux
                  </h3>

                  <p
                    style={{
                      color: "var(--color-muted)"
                    }}
                  >
                    Importez un fichier JSON provenant
                    de Microsoft Power Automate.
                  </p>

                </div>

              </Link>

              <Link
                to="/documentations"
                style={{ textDecoration: "none" }}
              >

                <div
                  className="card"
                  style={{
                    padding: 24,
                    cursor: "pointer",
                    height: "100%"
                  }}
                >

                  <FileText
                    color="var(--color-primary)"
                    size={34}
                  />

                  <h3
                    style={{
                      marginTop: 18,
                      marginBottom: 8
                    }}
                  >
                    Voir les documentations
                  </h3>

                  <p
                    style={{
                      color: "var(--color-muted)"
                    }}
                  >
                    Consultez,
                    modifiez
                    ou exportez
                    les documentations générées.
                  </p>

                </div>

              </Link>

            </div>

          </div>
          {/* ===============================
                 ACTIVITE RECENTE
          =============================== */}

          <div
            className="card"
            style={{
              padding: 30
            }}
          >
            <div
              className="row"
              style={{
                justifyContent: "space-between",
                alignItems: "center",
                marginBottom: 28
              }}
            >
              <div>
                <h2>Activité récente</h2>

                <p
                  style={{
                    marginTop: 6,
                    color: "var(--color-muted)"
                  }}
                >
                  Les dernières documentations générées sur la plateforme.
                </p>
              </div>

              <Link
                to="/documentations"
                style={{
                  color: "var(--color-primary)",
                  fontWeight: 600,
                  fontSize: 14
                }}
              >
                Voir tout →
              </Link>
            </div>

            {stats.recentDocumentations.length === 0 ? (
              <EmptyState
                icon={FileText}
                title="Aucune documentation disponible"
                description="Commencez par importer un flux Power Automate afin de générer automatiquement sa documentation."
                action={
                  <Link to="/importer">
                    <Button>
                      Importer un flux
                    </Button>
                  </Link>
                }
              />
            ) : (
              <div
                className="stack"
                style={{
                  gap: 18
                }}
              >
                {stats.recentDocumentations.map((doc) => (
                  <Link
                    key={doc.id}
                    to={`/documentations/${doc.id}`}
                    style={{
                      textDecoration: "none",
                      color: "inherit"
                    }}
                  >
                    <div
                      className="card"
                      style={{
                        padding: 20,
                        border: "1px solid var(--color-border)",
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "center",
                        gap: 20
                      }}
                    >
                      {/* Partie gauche */}

                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: 18,
                          minWidth: 0
                        }}
                      >
                        <div
                          style={{
                            width: 54,
                            height: 54,
                            borderRadius: 16,
                            background: "var(--color-primary-soft)",
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "center",
                            flexShrink: 0
                          }}
                        >
                          <FileText
                            size={24}
                            color="var(--color-primary)"
                          />
                        </div>

                        <div
                          style={{
                            minWidth: 0
                          }}
                        >
                          <div
                            style={{
                              fontSize: 15,
                              fontWeight: 700,
                              marginBottom: 6,
                              whiteSpace: "nowrap",
                              overflow: "hidden",
                              textOverflow: "ellipsis"
                            }}
                          >
                            {doc.title}
                          </div>

                          <div
                            style={{
                              fontSize: 13,
                              color: "var(--color-muted)"
                            }}
                          >
                            Flux :

                            <strong
                              style={{
                                color: "var(--color-ink)"
                              }}
                            >
                              {" "}
                              {doc.flowName}
                            </strong>
                          </div>
                        </div>
                      </div>

                      {/* Partie droite */}

                      <div
                        style={{
                          display: "flex",
                          alignItems: "center",
                          gap: 20,
                          flexShrink: 0
                        }}
                      >
                        <div
                          style={{
                            textAlign: "right"
                          }}
                        >
                          <div
                            style={{
                              fontSize: 12,
                              color: "var(--color-muted)"
                            }}
                          >
                            Dernière modification
                          </div>

                          <div
                            style={{
                              fontWeight: 600,
                              fontSize: 13
                            }}
                          >
                            {new Date(
                              doc.updatedAtUtc
                            ).toLocaleDateString(
                              "fr-FR",
                              {
                                day: "2-digit",
                                month: "short",
                                year: "numeric"
                              }
                            )}
                          </div>
                        </div>

                        <StatusBadge
                          status={doc.status}
                        />
                      </div>
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </div>

        </>
      )}
    </div>
  );
}
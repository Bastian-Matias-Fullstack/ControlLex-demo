using System;
using System.Collections.Generic;

namespace Aplicacion.Servicios.Demo
{
    public static class DemoResetBaseline
    {
        public static List<DemoCasoSeedItem> GetCasosBase()
        {
            return new List<DemoCasoSeedItem>
            {
                // =========================
                // PENDIENTES (5)
                // =========================
                new DemoCasoSeedItem
                {
                    Titulo = "Incumplimiento contractual en obra pública",
                    Descripcion = "Revisión de cláusulas contractuales, análisis de incumplimientos del mandante y preparación de estrategia legal inicial.",
                    NombreCliente = "Constructora Andina SpA",
                    TipoCaso = 3,
                    FechaCreacion = new DateTime(2026, 2, 19, 9, 15, 0),
                    Estado = "Pendiente",
                    ClienteId = 1,
                    FechaCambioEstado = null,
                    FechaCierre = null,
                    MotivoCierre = null,
                    CreatedBy = "seed",
                    ModifiedBy = null,
                    UpdatedAt = null
                },
                new DemoCasoSeedItem
                {
                    Titulo = "Revisión de contrato de prestación de servicios",
                    Descripcion = "Análisis integral de contrato marco, riesgos asociados, obligaciones recíprocas y propuesta de observaciones jurídicas.",
                    NombreCliente = "Servicios Jurídicos Altamira Ltda.",
                    TipoCaso = 3,
                    FechaCreacion = new DateTime(2026, 2, 20, 10, 0, 0),
                    Estado = "Pendiente",
                    ClienteId = 4,
                    FechaCambioEstado = null,
                    FechaCierre = null,
                    MotivoCierre = null,
                    CreatedBy = "seed",
                    ModifiedBy = null,
                    UpdatedAt = null
                },
                new DemoCasoSeedItem
                {
                    Titulo = "Preparación de demanda laboral por despido indirecto",
                    Descripcion = "Recopilación de antecedentes, revisión documental y estructuración inicial de acción laboral por incumplimientos graves del empleador.",
                    NombreCliente = "Inversiones Los Robles Ltda.",
                    TipoCaso = 1,
                    FechaCreacion = new DateTime(2026, 2, 21, 11, 20, 0),
                    Estado = "Pendiente",
                    ClienteId = 2,
                    FechaCambioEstado = null,
                    FechaCierre = null,
                    MotivoCierre = null,
                    CreatedBy = "seed",
                    ModifiedBy = null,
                    UpdatedAt = null
                },
                new DemoCasoSeedItem
                {
                    Titulo = "Redacción de querella por apropiación indebida",
                    Descripcion = "Preparación de escrito penal inicial, revisión de antecedentes contables y definición de hechos jurídicamente relevantes.",
                    NombreCliente = "Comercializadora Sur Global SpA",
                    TipoCaso = 2,
                    FechaCreacion = new DateTime(2026, 2, 22, 8, 45, 0),
                    Estado = "Pendiente",
                    ClienteId = 3,
                    FechaCambioEstado = null,
                    FechaCierre = null,
                    MotivoCierre = null,
                    CreatedBy = "seed",
                    ModifiedBy = null,
                    UpdatedAt = null
                },
                new DemoCasoSeedItem
                {
                    Titulo = "Revisión de bases y anexos de licitación pública",
                    Descripcion = "Evaluación de bases administrativas y técnicas para identificar riesgos contractuales y observaciones previas a la postulación.",
                    NombreCliente = "Inmobiliaria Costa Norte SpA",
                    TipoCaso = 3,
                    FechaCreacion = new DateTime(2026, 2, 23, 9, 10, 0),
                    Estado = "Pendiente",
                    ClienteId = 8,
                    FechaCambioEstado = null,
                    FechaCierre = null,
                    MotivoCierre = null,
                    CreatedBy = "seed",
                    ModifiedBy = null,
                    UpdatedAt = null
                },

                // =========================
                // EN PROCESO (5)
                // =========================
                new DemoCasoSeedItem
                {
                    Titulo = "Demanda laboral por despido injustificado",
                    Descripcion = "Revisión de antecedentes, cálculo preliminar de prestaciones adeudadas y preparación de estrategia procesal.",
                    NombreCliente = "Fundación Apoyo Social Horizonte",
                    TipoCaso = 1,
                    FechaCreacion = new DateTime(2026, 2, 19, 9, 40, 0),
                    Estado = "EnProceso",
                    ClienteId = 10,
                    FechaCambioEstado = new DateTime(2026, 2, 20, 15, 30, 0),
                    FechaCierre = null,
                    MotivoCierre = null,
                    CreatedBy = "seed",
                    ModifiedBy = "seed",
                    UpdatedAt = new DateTime(2026, 2, 20, 15, 30, 0)
                },
                new DemoCasoSeedItem
                {
                    Titulo = "Cobranza judicial de facturas impagas",
                    Descripcion = "Gestión judicial para recuperación de montos adeudados, revisión de respaldo documental y seguimiento de estado procesal.",
                    NombreCliente = "Transporte y Logística Patagonia SpA",
                    TipoCaso = 5,
                    FechaCreacion = new DateTime(2026, 2, 18, 14, 0, 0),
                    Estado = "EnProceso",
                    ClienteId = 5,
                    FechaCambioEstado = new DateTime(2026, 2, 19, 10, 15, 0),
                    FechaCierre = null,
                    MotivoCierre = null,
                    CreatedBy = "seed",
                    ModifiedBy = "seed",
                    UpdatedAt = new DateTime(2026, 2, 19, 10, 15, 0)
                },
                new DemoCasoSeedItem
                {
                    Titulo = "Negociación por incumplimiento contractual",
                    Descripcion = "Intercambio de observaciones, análisis de incumplimientos recíprocos y propuesta de salida negociada para evitar litigio.",
                    NombreCliente = "Agroexportadora Valle Verde SpA",
                    TipoCaso = 3,
                    FechaCreacion = new DateTime(2026, 2, 17, 12, 30, 0),
                    Estado = "EnProceso",
                    ClienteId = 6,
                    FechaCambioEstado = new DateTime(2026, 2, 18, 16, 45, 0),
                    FechaCierre = null,
                    MotivoCierre = null,
                    CreatedBy = "seed",
                    ModifiedBy = "seed",
                    UpdatedAt = new DateTime(2026, 2, 18, 16, 45, 0)
                },
                new DemoCasoSeedItem
                {
                    Titulo = "Regularización de deuda con proveedor crítico",
                    Descripcion = "Seguimiento de obligaciones vencidas, revisión de documentos mercantiles y negociación de calendario de pago.",
                    NombreCliente = "Retail Central Plaza S.A.",
                    TipoCaso = 5,
                    FechaCreacion = new DateTime(2026, 2, 24, 9, 0, 0),
                    Estado = "EnProceso",
                    ClienteId = 9,
                    FechaCambioEstado = new DateTime(2026, 2, 25, 11, 10, 0),
                    FechaCierre = null,
                    MotivoCierre = null,
                    CreatedBy = "seed",
                    ModifiedBy = "seed",
                    UpdatedAt = new DateTime(2026, 2, 25, 11, 10, 0)
                },
                new DemoCasoSeedItem
                {
                    Titulo = "Investigación interna por fraude documental",
                    Descripcion = "Levantamiento de antecedentes, revisión de documentos internos y coordinación de medidas de resguardo jurídico.",
                    NombreCliente = "Tecnología Nimbus Chile Ltda.",
                    TipoCaso = 2,
                    FechaCreacion = new DateTime(2026, 2, 25, 10, 20, 0),
                    Estado = "EnProceso",
                    ClienteId = 7,
                    FechaCambioEstado = new DateTime(2026, 2, 26, 13, 40, 0),
                    FechaCierre = null,
                    MotivoCierre = null,
                    CreatedBy = "seed",
                    ModifiedBy = "seed",
                    UpdatedAt = new DateTime(2026, 2, 26, 13, 40, 0)
                },

                // =========================
                // CERRADOS (5)
                // =========================
                new DemoCasoSeedItem
                {
                    Titulo = "Cierre por acuerdo de pago de facturas vencidas",
                    Descripcion = "Cierre de gestión de cobranza tras validación de acuerdo firmado, calendario de pago aceptado y constancia documental.",
                    NombreCliente = "Constructora Andina SpA",
                    TipoCaso = 5,
                    FechaCreacion = new DateTime(2026, 2, 10, 9, 0, 0),
                    Estado = "Cerrado",
                    ClienteId = 1,
                    FechaCambioEstado = new DateTime(2026, 2, 12, 12, 0, 0),
                    FechaCierre = new DateTime(2026, 2, 14, 17, 30, 0),
                    MotivoCierre = "Acuerdo de pago firmado y validado por ambas partes.",
                    CreatedBy = "seed",
                    ModifiedBy = "seed",
                    UpdatedAt = new DateTime(2026, 2, 14, 17, 30, 0)
                },
                new DemoCasoSeedItem
                {
                    Titulo = "Cierre por conciliación laboral extrajudicial",
                    Descripcion = "Se logró acuerdo de término, revisión de cláusulas de conciliación y cierre documentado del caso.",
                    NombreCliente = "Inversiones Los Robles Ltda.",
                    TipoCaso = 1,
                    FechaCreacion = new DateTime(2026, 2, 8, 10, 10, 0),
                    Estado = "Cerrado",
                    ClienteId = 2,
                    FechaCambioEstado = new DateTime(2026, 2, 11, 15, 0, 0),
                    FechaCierre = new DateTime(2026, 2, 13, 16, 45, 0),
                    MotivoCierre = "Conciliación aprobada y caso finalizado.",
                    CreatedBy = "seed",
                    ModifiedBy = "seed",
                    UpdatedAt = new DateTime(2026, 2, 13, 16, 45, 0)
                },
                new DemoCasoSeedItem
                {
                    Titulo = "Cierre por suscripción de anexo correctivo contractual",
                    Descripcion = "Cierre de revisión contractual luego de acordar anexo modificatorio y correcciones observadas por ambas partes.",
                    NombreCliente = "Inmobiliaria Costa Norte SpA",
                    TipoCaso = 3,
                    FechaCreacion = new DateTime(2026, 2, 9, 11, 25, 0),
                    Estado = "Cerrado",
                    ClienteId = 8,
                    FechaCambioEstado = new DateTime(2026, 2, 12, 10, 50, 0),
                    FechaCierre = new DateTime(2026, 2, 15, 18, 0, 0),
                    MotivoCierre = "Se firmó anexo correctivo y se dio por resuelto el conflicto contractual.",
                    CreatedBy = "seed",
                    ModifiedBy = "seed",
                    UpdatedAt = new DateTime(2026, 2, 15, 18, 0, 0)
                },
                new DemoCasoSeedItem
                {
                    Titulo = "Cierre por acuerdo reparatorio en denuncia penal",
                    Descripcion = "Cierre de gestión penal tras acuerdo reparatorio aceptado y revisión de antecedentes finales.",
                    NombreCliente = "Agroexportadora Valle Verde SpA",
                    TipoCaso = 2,
                    FechaCreacion = new DateTime(2026, 2, 7, 9, 35, 0),
                    Estado = "Cerrado",
                    ClienteId = 6,
                    FechaCambioEstado = new DateTime(2026, 2, 10, 14, 20, 0),
                    FechaCierre = new DateTime(2026, 2, 12, 17, 10, 0),
                    MotivoCierre = "Acuerdo reparatorio aceptado y caso penal archivado.",
                    CreatedBy = "seed",
                    ModifiedBy = "seed",
                    UpdatedAt = new DateTime(2026, 2, 12, 17, 10, 0)
                },
                new DemoCasoSeedItem
                {
                    Titulo = "Cierre por cumplimiento de plan de regularización de deuda",
                    Descripcion = "Cierre de cobranza luego de verificar cumplimiento íntegro del plan pactado y respaldo documental conforme.",
                    NombreCliente = "Retail Central Plaza S.A.",
                    TipoCaso = 5,
                    FechaCreacion = new DateTime(2026, 2, 6, 8, 50, 0),
                    Estado = "Cerrado",
                    ClienteId = 9,
                    FechaCambioEstado = new DateTime(2026, 2, 9, 13, 15, 0),
                    FechaCierre = new DateTime(2026, 2, 11, 16, 20, 0),
                    MotivoCierre = "Pago total verificado y caso cerrado por cumplimiento.",
                    CreatedBy = "seed",
                    ModifiedBy = "seed",
                    UpdatedAt = new DateTime(2026, 2, 11, 16, 20, 0)
                }
            };
        }
    }
}

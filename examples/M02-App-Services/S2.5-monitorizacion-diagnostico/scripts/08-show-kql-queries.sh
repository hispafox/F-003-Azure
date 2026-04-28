#!/usr/bin/env bash
# 08 — Imprime queries KQL útiles para copiar y pegar en
# Portal -> Application Insights -> Logs.
# Slide 16 — KQL queries básicas.

cat <<'EOF'
==========================================================
  Queries KQL para Application Insights / Log Analytics
==========================================================
  Pegar en Portal -> tu App Insights -> Logs.
==========================================================

----------------------------------------------------------
 1) Top 10 peticiones más lentas (últimas 24h)
----------------------------------------------------------
requests
| where timestamp > ago(24h)
| where duration > 1000
| project timestamp, name, duration, resultCode, client_IP, operation_Id
| top 10 by duration desc

----------------------------------------------------------
 2) Tasa de errores por hora (últimas 24h)
----------------------------------------------------------
requests
| where timestamp > ago(24h)
| summarize total = count(),
            errores = countif(resultCode startswith "5")
            by bin(timestamp, 1h)
| extend errorRate = round(errores * 100.0 / total, 2)
| project timestamp, total, errores, errorRate
| order by timestamp asc

----------------------------------------------------------
 3) Excepciones agrupadas por tipo (últimas 24h)
----------------------------------------------------------
exceptions
| where timestamp > ago(24h)
| summarize count() by type, outerMessage
| order by count_ desc
| take 20

----------------------------------------------------------
 4) Pedidos creados por prioridad (custom metric)
----------------------------------------------------------
customMetrics
| where timestamp > ago(24h)
| where name == "demo.orders.created"
| extend prioridad = tostring(customDimensions.priority)
| summarize sum(value) by prioridad, bin(timestamp, 15m)
| render timechart

----------------------------------------------------------
 5) Logs estructurados con PiiScrubber (verificar redacciones)
----------------------------------------------------------
traces
| where timestamp > ago(1h)
| where message contains "Mensaje recibido"
| project timestamp, message, customDimensions
| top 50 by timestamp desc

----------------------------------------------------------
 6) Dependency calls que han fallado
----------------------------------------------------------
dependencies
| where timestamp > ago(24h)
| where success == false
| summarize count(), avg(duration) by target, type, name, resultCode
| order by count_ desc

----------------------------------------------------------
 7) Operación end-to-end por operation_Id (correlation)
----------------------------------------------------------
//  pega un operation_Id concreto en la cláusula where
let id = "<pegar-operation-id-aqui>";
union requests, dependencies, exceptions, traces
| where operation_Id == id
| project timestamp, itemType, name, duration, resultCode, message
| order by timestamp asc

==========================================================
EOF

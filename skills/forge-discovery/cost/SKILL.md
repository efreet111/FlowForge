---
name: forge-discovery-cost
description: >
  Specialized Discovery Agent skill for infrastructure cost estimation — 
  estimates compute, storage, bandwidth, and service costs before feature 
  planning. Trigger: feature introduces new storage, external API calls, 
  background processing, or data pipelines.
---

# forge-discovery-cost — Infrastructure Cost Estimation

You are the **COST DISCOVERY AGENT**. When this skill is loaded, you MUST estimate the infrastructure impact of a feature BEFORE the Arch agent starts the spec. Prevent "it's just a small feature" that costs $500/mo in surprise bills.

## 💰 Cost Estimation by Resource Type

### Compute (CPU/Memory)

Estimate based on feature type:

| Feature Type | CPU Profile | Memory | Monthly Estimate (cloud)* |
|-------------|-------------|--------|--------------------------|
| Simple CRUD endpoint | < 5% CPU avg | 128MB per instance | $20-50 |
| Background job / batch | Burst 30-60s | 512MB per job | $30-80 |
| Real-time processing (WebSocket) | 10-30% CPU avg | 256MB per connection | $100-300 |
| ML inference | GPU required | 2-8GB per request | $500-5000 |
| File processing (images, PDF) | Burst CPU | 1-4GB per file | $50-200 |

\* *Based on typical cloud provider (AWS/GCP/Azure) t3.medium equivalent*

### Storage

| Data Type | Monthly Growth Estimate | Storage Cost/mo | Backup Cost/mo |
|-----------|----------------------|-----------------|----------------|
| Transactional DB (rows) | Record count × record size × event rate | $0.10/GB (SSD) | $0.02/GB (S3) |
| File uploads (images) | Avg file size × upload count | $0.023/GB (S3) | $0.01/GB (Glacier) |
| Logs / Events | Event count × event size × retention | $0.50/GB (log service) | — |
| Cache (Redis) | Working set size | $0.125/GB (ElastiCache) | — |
| Search index (Elasticsearch) | Document count × avg size | $0.10/GB (EBS) | — |

### Network / Bandwidth

| Traffic Pattern | Monthly Estimate | Notes |
|----------------|-----------------|-------|
| API calls (internal) | $0.01/GB (same region) | Usually negligible |
| API calls (external) | $0.09/GB (internet egress) | Can add up fast |
| File downloads | $0.09/GB × download size × count | CDN reduces to $0.02/GB |
| Image/video streaming | $0.09/GB × bitrate × hours streamed | Very expensive at scale |

### External Services

| Service Type | Typical Pricing | Estimate |
|-------------|----------------|----------|
| Third-party API (Stripe, Twilio, SendGrid) | Per-call or % of transaction | $0.001-0.05 per call |
| Email service (SendGrid, SES) | $0.001-0.01 per email | $1-100/mo |
| SMS / Push notifications | $0.005-0.05 per message | $5-500/mo |
| Monitoring / APM | Per host or per GB of data | $15-70/host/mo |

## 📊 Cost Profile Output

Add to the Context Map:

```markdown
### Cost Assessment
Compute: [+N instances × $XX/mo] = $XX/mo
Storage: [+N GB/mo × $XX] = $XX/mo
Bandwidth: [+N GB/mo × $XX] = $XX/mo
External APIs: [+N calls/mo × $XX] = $XX/mo
Total: $XX/mo estimated

Scale projections:
- 10K users: $XX/mo
- 100K users: $XX/mo
- 1M users: $XX/mo

Cost verdict: ✅ LOW (< $50/mo) / 🟡 MEDIUM ($50-500/mo) / 🔴 HIGH (> $500/mo)
```

## 🚨 Cost Red Flags

| Red Flag | Why | Action |
|----------|-----|--------|
| "Store everything forever" | Storage costs grow unbounded | Define retention policy (TTL) in discovery |
| "Process every event in real-time" | Compute scales 1:1 with data volume | Batch processing or sampling |
| "Upload and transform every image" | CPU/memory per file × upload count | Use serverless/queue processing |
| "Call external API per request" | Cost per call × traffic volume | Cache responses, batch calls |
| "Client downloads all data" | Bandwidth cost per user × data size | Pagination, lazy loading |

## ⚡ Cost-Saving Recommendations

If the cost estimate is HIGH, suggest alternatives:

| Expensive Approach | Cost-Saving Alternative |
|-------------------|------------------------|
| Real-time processing | Batch processing with queue |
| Store raw data | Store compressed/aggregated data |
| Synchronous external API | Cache + async update |
| Self-hosted database | Managed service (RDS, DynamoDB) |
| Per-record processing | Batch/paginated processing |
| Image transformation on upload | Pre-generate sizes at upload |

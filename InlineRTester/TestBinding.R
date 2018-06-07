print("Starting script")
print(Sys.time())

# .libPaths()
.libPaths( c( .libPaths(), "C:/himss/R/lib") )
# .libPaths()

setwd("C:/himss/R")
# getwd()
print(Sys.time())

# this seems to be needed to make selectData work
library(methods)
library(healthcareai)
library(RODBC)
## Install dplyr package with this code: install.packages("dplyr")
library(tidyverse)

print("loaded libraries")
print(Sys.time())

# Pull data from EDW ===========================
con_str <-  build_connection_string(server = "(local)", database = "SAM")
query <- "
SELECT
summ.FacilityAccountID
,summ.AdmitAgeNBR
,summ.TemperatureMaxNBR
,summ.PulseMaxNBR
,summ.O2SatMinNBR
,summ.SBPMinNBR
,summ.EDVisitsPrior90DaysNBR
,summ.AntibioticPrior7DaysFLG
,summ.SepsisFLG
FROM SAM.Sepsis.EWSSummary summ
"
con <- RODBC::odbcDriverConnect(con_str)
d <- RODBC::sqlQuery(con, query)
glimpse(d)
print("Loaded data from EDW")
print(Sys.time())

d_clean <- prep_data(d, FacilityAccountID, outcome = SepsisFLG)

# Train model ======================
# Only need this block to train model the first time.
if (0==1) {
  print("Training model")
  d_clean <- prep_data(d, FacilityAccountID, outcome = SepsisFLG)
  m <- flash_models(d = d_clean, outcome = SepsisFLG, models = "rf")
  save_models(x = m, filename = "sepsis_demo_model_2018-05-04.RDS")
}

# Generate predictions ======================
print("Loading model and generating predictions")
m <- load_models(filename = "sepsis_demo_model_2018-05-04.RDS")

# Pretend the new data is the first 10 patients.
d_pred <- predict(m, newdata = d_clean[1:10, ], prepdata = FALSE)
print("Finished model")

# Get top factors =======================
v <- get_variable_importance(models = m)


top_factor_row <- function(v, n) {
  top_factors <- sample(x = v$variable[1:7],
                        size = 5,
                        replace = FALSE,
                        prob = v$importance[1:7]/100)
  return(top_factors)
}

hack_top_factors <- function(d_pred, variable_importance) {
  top_factors <- replicate(nrow(d_pred), top_factor_row(variable_importance)) %>%
    t() %>%
    as.tibble()
  names(top_factors) <- c("Factor1TXT", "Factor2TXT", "Factor3TXT", "Factor4TXT", "Factor5TXT")
  d <- bind_cols(d_pred, top_factors)
  return(d)
}

d_pred <- hack_top_factors(d_pred, v)


## Creates a new column called RankedRiskFactor1DSC and pushes it to d_pred data frame
d_pred <- mutate(
  d_pred,
  RankedRiskFactor1DSC = case_when(
    Factor1TXT == "AdmitAgeNBR" ~ paste("Age: ", AdmitAgeNBR),
    Factor1TXT == "TemperatureMaxNBR" ~ paste("Temperature: ", TemperatureMaxNBR),
    Factor1TXT == "PulseMaxNBR"        ~ paste("Pulse: ", PulseMaxNBR),
    Factor1TXT == "O2SatMinNBR"        ~ paste("O2 Saturation: ", O2SatMinNBR),
    Factor1TXT == "SBPMinNBR"        ~ paste("Systolic BP: ", SBPMinNBR),
    Factor1TXT == "EDVisitsPrior90DaysNBR"        ~ paste("ED Visits (90-days): ", EDVisitsPrior90DaysNBR),
    Factor1TXT == "AntibioticPrior7DaysFLG_Y"        ~ paste("No Prior Abx (7-days)")
  )
)

## Creates a new column called RankedRiskFactor2DSC and pushes it to d_pred data frame
d_pred <- mutate(
  d_pred,
  RankedRiskFactor2DSC = case_when(
    Factor2TXT == "AdmitAgeNBR" ~ paste("Age: ", AdmitAgeNBR),
    Factor2TXT == "TemperatureMaxNBR" ~ paste("Temperature: ", TemperatureMaxNBR),
    Factor2TXT == "PulseMaxNBR"        ~ paste("Pulse: ", PulseMaxNBR),
    Factor2TXT == "O2SatMinNBR"        ~ paste("O2 Saturation: ", O2SatMinNBR),
    Factor2TXT == "SBPMinNBR"        ~ paste("Systolic BP: ", SBPMinNBR),
    Factor2TXT == "EDVisitsPrior90DaysNBR"        ~ paste("ED Visits (90-days): ", EDVisitsPrior90DaysNBR),
    Factor2TXT == "AntibioticPrior7DaysFLG_Y"        ~ paste("No Prior Abx (7-days)")
  )
)

## Creates a new column called RankedRiskFactor3DSC and pushes it to d_pred data frame
d_pred <- mutate(
  d_pred,
  RankedRiskFactor3DSC = case_when(
    Factor3TXT == "AdmitAgeNBR" ~ paste("Age: ", AdmitAgeNBR),
    Factor3TXT == "TemperatureMaxNBR" ~ paste("Temperature: ", TemperatureMaxNBR),
    Factor3TXT == "PulseMaxNBR"        ~ paste("Pulse: ", PulseMaxNBR),
    Factor3TXT == "O2SatMinNBR"        ~ paste("O2 Saturation: ", O2SatMinNBR),
    Factor3TXT == "SBPMinNBR"        ~ paste("Systolic BP: ", SBPMinNBR),
    Factor3TXT == "EDVisitsPrior90DaysNBR"        ~ paste("ED Visits (90-days): ", EDVisitsPrior90DaysNBR),
    Factor3TXT == "AntibioticPrior7DaysFLG_Y"        ~ paste("No Prior Abx (7-days)")
  )
)

## Creates a new column called RankedRiskFactor4DSC and pushes it to d_pred data frame
d_pred <- mutate(
  d_pred,
  RankedRiskFactor4DSC = case_when(
    Factor4TXT == "AdmitAgeNBR" ~ paste("Age: ", AdmitAgeNBR),
    Factor4TXT == "TemperatureMaxNBR" ~ paste("Temperature: ", TemperatureMaxNBR),
    Factor4TXT == "PulseMaxNBR"        ~ paste("Pulse: ", PulseMaxNBR),
    Factor4TXT == "O2SatMinNBR"        ~ paste("O2 Saturation: ", O2SatMinNBR),
    Factor4TXT == "SBPMinNBR"        ~ paste("Systolic BP: ", SBPMinNBR),
    Factor4TXT == "EDVisitsPrior90DaysNBR"        ~ paste("ED Visits (90-days): ", EDVisitsPrior90DaysNBR),
    Factor4TXT == "AntibioticPrior7DaysFLG_Y"        ~ paste("No Prior Abx (7-days)")
  )
)

## Creates a new column called RankedRiskFactor5DSC and pushes it to d_pred data frame
d_pred <- mutate(
  d_pred,
  RankedRiskFactor5DSC = case_when(
    Factor5TXT == "AdmitAgeNBR" ~ paste("Age: ", AdmitAgeNBR),
    Factor5TXT == "TemperatureMaxNBR" ~ paste("Temperature: ", TemperatureMaxNBR),
    Factor5TXT == "PulseMaxNBR"        ~ paste("Pulse: ", PulseMaxNBR),
    Factor5TXT == "O2SatMinNBR"        ~ paste("O2 Saturation: ", O2SatMinNBR),
    Factor5TXT == "SBPMinNBR"        ~ paste("Systolic BP: ", SBPMinNBR),
    Factor5TXT == "EDVisitsPrior90DaysNBR"        ~ paste("ED Visits (90-days): ", EDVisitsPrior90DaysNBR),
    Factor5TXT == "AntibioticPrior7DaysFLG_Y"        ~ paste("No Prior Abx (7-days)")
  )
)

## Creates a new column called RelativeRiskValueDSC and pushes it to d_pred data frame
d_pred <- mutate(d_pred,
                 RelativeRiskValueDSC = case_when(
                   round(predicted_SepsisFLG  / mean(predicted_SepsisFLG ), 1) >= 1.0 ~ paste(round(
                     predicted_SepsisFLG  / mean(predicted_SepsisFLG ), 1
                   ), " x"),
                   round(predicted_SepsisFLG  / mean(predicted_SepsisFLG ), 1) < 1.0 ~ paste(round(1 /
                                                                                                     (
                                                                                                       predicted_SepsisFLG  / mean(predicted_SepsisFLG )
                                                                                                     ), 1), " x")
                 ))

## Creates a new column called RelativeRiskHigherLowerDSC and pushes it to d_pred data frame
d_pred <- mutate(
  d_pred,
  RelativeRiskHigherLowerDSC = case_when(
    round(predicted_SepsisFLG  / mean(predicted_SepsisFLG ), 1) >= 1.0 ~ "higher than dept. avg.",
    round(predicted_SepsisFLG  / mean(predicted_SepsisFLG ), 1) < 1.0 ~ "lower than dept. avg."
  )
)

## Creates a new column called AlertPopUpFLG and pushes it to d_pred data frame
d_pred <- mutate(d_pred,
                 AlertPopUpFLG = case_when(
                   round(predicted_SepsisFLG  / mean(predicted_SepsisFLG ), 0) >= 3   ~ "Y",
                   round(predicted_SepsisFLG  / mean(predicted_SepsisFLG ), 0) < 3   ~ "N"
                 ))

## Creates a new column called RiskLastUpdatedDSC and pushes it to d_pred data frame
d_pred <- mutate(
  d_pred,
  LastLoadDTS = Sys.time(),
  BindingID = 0,
  BindingNM = "R",
  RiskLastUpdatedDSC = paste("Risk Last Updated: ",LastLoadDTS)
)

## Rearranges the data frame into the preferred order of columns
d_pred <- select(d_pred,BindingID,BindingNM,LastLoadDTS,FacilityAccountID
                 ,predicted_SepsisFLG  ,Factor1TXT,Factor2TXT,Factor3TXT,Factor4TXT
                 ,Factor5TXT,AdmitAgeNBR,TemperatureMaxNBR
                 ,PulseMaxNBR,O2SatMinNBR,SBPMinNBR,EDVisitsPrior90DaysNBR
                 ,AntibioticPrior7DaysFLG_Y,SepsisFLG,RankedRiskFactor1DSC
                 ,RankedRiskFactor2DSC,RankedRiskFactor3DSC,RankedRiskFactor4DSC
                 ,RankedRiskFactor5DSC
                 ,RelativeRiskValueDSC,RelativeRiskHigherLowerDSC,AlertPopUpFLG
                 ,RiskLastUpdatedDSC
)


## Push output to EDW
## Note: Use this CREATE TABLE statement in SSMS to create the output table (already done in hcs-gm0001)

# CREATE TABLE SAM.Sepsis.EWSPredictionsBASE (
#   BindingID float
#   ,BindingNM varchar(255)
#   ,LastLoadDTS datetime2
#   ,FacilityAccountID varchar(255)
#   ,predicted_SepsisFLG  decimal(38, 2)
#   ,Factor1TXT varchar(255)
#   ,Factor2TXT varchar(255)
#   ,Factor3TXT varchar(255)
#   ,Factor4TXT varchar(255)
#   ,Factor5TXT varchar(255)
#   ,AdmitAgeNBR int
#   ,TemperatureMaxNBR decimal(38, 1)
#   ,PulseMaxNBR int
#   ,O2SatMinNBR int
#   ,SBPMinNBR int
#   ,EDVisitsPrior90DaysNBR int
#   ,AntibioticPrior7DaysFLG_Y varchar(255)
#   ,SepsisFLG varchar(255)
#   ,RankedRiskFactor1DSC varchar(255)
#   ,RankedRiskFactor2DSC varchar(255)
#   ,RankedRiskFactor3DSC varchar(255)
#   ,RankedRiskFactor4DSC varchar(255)
#   ,RankedRiskFactor5DSC varchar(255)
#   ,RelativeRiskValueDSC varchar(255)
#   ,RelativeRiskHigherLowerDSC varchar(255)
#   ,AlertPopUpFLG varchar(255)
#   ,RiskLastUpdatedDSC varchar(255)
#   ,LastCalculatedDTS datetime2
# )

d_pred$LastCalculatedDTS<-Sys.time()

print("saving output")

RODBC::sqlSave(con, d_pred, "Sepsis.EWSPredictionsBASE", append = TRUE,
               rownames = FALSE)


RODBC::odbcClose(con)

print("Finished script")


# SheepHerding
Repository containing the source code behind the manuscript A Hybrid Model for Simulating Grazing Herds in Real-time published in the Computer Animation and Virtual Worlds journal (https://onlinelibrary.wiley.com/doi/abs/10.1002/cav.1914).

# Authors
Jure Demšar, Faculty of Computer and Information Science, University of Ljubljana  
Will Blewitt, School of Computing, Electronics and Maths, Coventry University  
Iztok Lebar Bajec, Faculty of Computer and Information Science, University of Ljubljana

# Abstract
Computer simulations of animal groups are usually performed via individual based modelling, where simulated animals are designed on the level of individuals. With this approach developers are able to capture behavioural nuances of real animals. However, modelling each individual as its own entity has the downside of having a high computational cost, meaning that individual based models are usually not suitable for real-time simulations of very large groups. A common alternative approach is flow based modelling, where the dynamics of animal congregations are designed on the group level. This enables researchers to create real-time simulations of massive phenomena at the cost of biological authenticity. A novel approach called hybrid modelling tries to mix the best of both worlds - precision of individual based models and speed of flow based ones. An unknown surrounding hybrid models is the question of their biological authenticity and relevance. In the presented study, we develop a hybrid model for the simulation of herds of grazing sheep. Through Bayesian data analysis we show that we can encompass several aspects of real-world sheep behaviour. Our hybrid model is also extremely efficient, capable of simulating herds of more than 1000 individuals in real-time without resorting to GPU execution.

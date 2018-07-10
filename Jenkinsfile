pipeline {
    agent none
    stages {
        stage('Build') {
            agent { label 'windows' }
            steps {
                echo 'Building..'
                bat returnStatus: true, script: 'msbuild AgMIPToMonicaConverter.sln /T:Rebuild /P:Configuration=Release /m"'
            }
        }
        stage('Test') {
            agent any
            steps {
                echo 'Testing..'
            }
        }
        stage('Deploy') {
            agent any
            steps {
                echo 'Deploying....'
            }
        }
    }
}